using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.Playwright;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using File = System.IO.File;

namespace ShortVideo.Claw.Browser;

internal static class Program
{
    private static string BaseDir => AppContext.BaseDirectory;

    /// <summary>控制文件路径，主进程写入 socket 路径供客户端读取。</summary>
    private static string CtlFilePath => Path.Combine(BaseDir, "ShortVideo.Claw-browser.ctl");

    /// <summary>主进程 socket 路径。放 Temp 避免 AF_UNIX 108 字节限制，用 BaseDir 哈希区分不同安装。</summary>
    private static string GetSocketPath(int pid)
    {
        var dir = Path.GetTempPath();
        var hash = Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(BaseDir)))
            .AsSpan(0, 8)
            .ToString();
        return Path.Combine(dir, $"olb-{hash}-{pid}.sock");
    }

    private static string GetSocketDirPrefix()
    {
        var hash = Convert
            .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(BaseDir)))
            .AsSpan(0, 8)
            .ToString();
        return Path.Combine(Path.GetTempPath(), $"olb-{hash}-");
    }

    /// <summary>从控制文件读取当前主进程的 socket 路径，不存在或读取失败返回 null。</summary>
    private static string? TryReadSocketPathFromCtl()
    {
        try
        {
            var path = File.ReadAllText(CtlFilePath).Trim();
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

    /// <summary>默认 true（不可见）。当 _forceVisible 为 true 时强制为 false；否则由 init/visible/headless 决定。</summary>
    private static bool _headless = true;
    /// <summary>为 true 时强制显示浏览器窗口，忽略 agent 的 headless/visible 设置。</summary>
    private static bool _forceVisible = false;
    private static string? _channel;
    private static IPlaywright? _pw;
    private static IBrowser? _browser;
    private static IPage? _page;
    private static readonly ConcurrentDictionary<
        string,
        (string Role, string? Name, int Nth)
    > RefMap = new();
    private static readonly SemaphoreSlim Lock = new(1, 1);

    private static void Log(string msg)
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            var path = Path.Combine(dir, "ShortVideo.Claw-browser.log");
            File.AppendAllText(path, $"{DateTime.Now:HH:mm:ss.fff} {msg}{Environment.NewLine}");
        }
        catch { }
        Console.Error.WriteLine(msg);
    }

    public static async Task<int> Main(string[] args)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Environment.Exit(0);
        };
        Log($"[main] args: [{string.Join(", ", args)}]");

        // 1) --master / --server: 主进程（nginx 风格），持有 PID 文件锁，维持浏览器，运行管道服务
        if (
            args.Contains("--master", StringComparer.OrdinalIgnoreCase)
            || args.Contains("--server", StringComparer.OrdinalIgnoreCase)
        )
        {
            return await RunAsMasterAsync(args).ConfigureAwait(false);
        }

        // 2) CLI 客户端：解析参数，通过 Unix domain socket 连接主进程
        var (json, parseErr) = CliParser.Parse(args);
        if (parseErr != null)
        {
            Console.Error.WriteLine(parseErr);
            return 1;
        }
        if (json == null)
        {
            Console.Error.WriteLine("usage: ShortVideo.Claw-browser [--master] <command> [args]");
            return 1;
        }

        // 尝试连接主进程；若失败则 spawn 主进程后重试
        await EnsureMasterRunningAsync().ConfigureAwait(false);
        return await RunClientAsync(json).ConfigureAwait(false);
    }

    /// <summary>主进程入口：持有 PID 文件锁，维持浏览器，对外提供 Unix domain socket 服务。</summary>
    private static async Task<int> RunAsMasterAsync(string[] args)
    {
        Log("[master] step 1: acquiring PID file lock");
        using var pidLock = PidLock.TryAcquire();
        if (pidLock == null)
        {
            Log("[master] PID lock held by another process, exit");
            return 0; // 已有主进程，直接退出
        }
        Log($"[master] step 2: PID={pidLock.Pid} lock acquired at {pidLock.Path}");

        LoadConfigFromExeDir();
        if (!OperatingSystem.IsWindows())
            _channel ??= "msedge";

        Log("[master] step 3: RunServerAsync");
        await RunServerAsync(pidLock.Pid).ConfigureAwait(false);
        Log("[master] step 4: done");
        return 0;
    }

    /// <summary>确保主进程在运行：若 socket 不可达则 spawn 主进程并等待就绪。用文件锁协调，无 Mutex。</summary>
    private static async Task EnsureMasterRunningAsync()
    {
        if (await TryConnectToMasterAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false))
            return;

        using var startLock = StartLock.TryAcquire();
        if (startLock != null)
        {
            if (await TryConnectToMasterAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false))
                return;
            var ok = await TryStartMasterAsync().ConfigureAwait(false);
            if (!ok)
                await WaitForServerReadyAsync().ConfigureAwait(false);
        }
        else
        {
            if (await TryConnectToMasterAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false))
                return;
            await WaitForServerReadyAsync().ConfigureAwait(false);
        }
    }

    /// <summary>尝试连接主进程 socket，用于快速检测主进程是否存活。不经过网络。</summary>
    private static async Task<bool> TryConnectToMasterAsync(TimeSpan timeout)
    {
        var path = TryReadSocketPathFromCtl();
        if (string.IsNullOrEmpty(path))
            return false;
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            using var _ = socket;
            await socket
                .ConnectAsync(new UnixDomainSocketEndPoint(path), cts.Token)
                .ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void LoadConfigFromExeDir()
    {
        var dir = AppContext.BaseDirectory;
        var path = Path.Combine(dir, "ShortVideo.Claw-browser.json");
        if (!File.Exists(path))
            return;
        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("forceVisible", out var fv) || root.TryGetProperty("force-visible", out fv))
            {
                _forceVisible = fv.GetBoolean();
                if (_forceVisible)
                    _headless = false;
            }
            if (root.TryGetProperty("channel", out var c))
                _channel = c.GetString();
        }
        catch
        { /* ignore */
        }
    }

    /// <summary>启动主进程（同 exe --master），并等待其 stdout 输出 "started"。失败时由 WaitForServerReadyAsync 轮询兜底。</summary>
    private static async Task<bool> TryStartMasterAsync()
    {
        var exe =
            Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "ShortVideo.Claw-browser.exe");
        if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
        {
            Console.Error.WriteLine($"ShortVideo.Claw-browser: exe not found: {exe}");
            return false;
        }
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exe,
            Arguments = "--master",
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        try
        {
            var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null)
                return false;

            _ = Task.Run(async () =>
            {
                try
                {
                    await proc.StandardError.ReadToEndAsync();
                }
                catch { }
            });
            using var reader = proc.StandardOutput;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (await reader.ReadLineAsync(cts.Token).ConfigureAwait(false) is { } line)
            {
                if (line.Trim().Equals("started", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ShortVideo.Claw-browser: start failed: {ex.Message}");
        }
        return false;
    }

    /// <summary>轮询等待主进程就绪：.ctl 文件存在且 socket 可连接即表示 master 存活。</summary>
    private static async Task WaitForServerReadyAsync()
    {
        for (var i = 0; i < 200; i++)
        {
            if (await TryConnectToMasterAsync(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false))
                return;
            await Task.Delay(50).ConfigureAwait(false);
        }
    }

    private static async Task<int> RunClientAsync(string jsonLine)
    {
        using var reqLock = ReqLock.WaitAndAcquire(timeoutMs: 30_000);
        if (reqLock == null)
        {
            Console.Error.WriteLine("ShortVideo.Claw-browser: request lock timeout.");
            return 1;
        }
        try
        {
            string? path = null;
            for (var i = 0; i < 50; i++)
            {
                path = TryReadSocketPathFromCtl();
                if (!string.IsNullOrEmpty(path))
                    break;
                await Task.Delay(100).ConfigureAwait(false);
            }
            if (string.IsNullOrEmpty(path))
            {
                Console.Error.WriteLine(
                    "ShortVideo.Claw-browser: control file not found, master may not be ready."
                );
                return 1;
            }
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await socket
                .ConnectAsync(new UnixDomainSocketEndPoint(path), cts.Token)
                .ConfigureAwait(false);
            await using var stream = new NetworkStream(socket, ownsSocket: true);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            var respReader = new StreamReader(stream, Encoding.UTF8);
            await writer.WriteLineAsync(jsonLine).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
            var resp = await respReader.ReadLineAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(resp))
                Console.WriteLine(resp);
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("ShortVideo.Claw-browser: connect timeout.");
            return 1;
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"ShortVideo.Claw-browser: {ex.Message}");
            return 1;
        }
    }

    private static async Task RunServerAsync(int masterPid)
    {
        Log("[server] RunServerAsync: start");
        var cts = new CancellationTokenSource();
        var watchdog = Task.Run(() => BrowserWatchdogAsync(cts), CancellationToken.None);
        try
        {
            Log("[server] RunServerAsync: entering SocketServerLoop");
            await SocketServerLoopAsync(masterPid, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            cts.Cancel();
            await CloseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>检测浏览器被用户关闭时，优雅退出主服务（触发 finally 清理 .ctl/.sock）。</summary>
    private static async Task BrowserWatchdogAsync(CancellationTokenSource cts)
    {
        while (!cts.Token.IsCancellationRequested)
        {
            await Task.Delay(2000, cts.Token).ConfigureAwait(false);
            if (_browser != null && !_browser.IsConnected)
            {
                cts.Cancel();
                return;
            }
        }
    }

    private static void CleanupStaleSocketFiles(int keepPid)
    {
        var ourPath = GetSocketPath(keepPid);
        var prefix = GetSocketDirPrefix();
        try
        {
            foreach (var f in Directory.GetFiles(Path.GetTempPath(), "olb-*-*.sock"))
            {
                if (
                    f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(f, ourPath, StringComparison.OrdinalIgnoreCase)
                )
                {
                    try
                    {
                        File.Delete(f);
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private static async Task SocketServerLoopAsync(int masterPid, CancellationToken ct)
    {
        Log("[server] SocketServerLoop: start");
        var socketPath = GetSocketPath(masterPid);
        CleanupStaleSocketFiles(masterPid);
        if (File.Exists(socketPath))
        {
            try
            {
                File.Delete(socketPath);
            }
            catch
            { /* 残留文件，继续尝试 */
            }
        }
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
        try
        {
            listener.Bind(new UnixDomainSocketEndPoint(socketPath));
            listener.Listen(1);
            Log("[server] SocketServerLoop: listening at " + socketPath);

            try
            {
                File.WriteAllText(CtlFilePath, socketPath);
            }
            catch (IOException ex)
            {
                Log("[server] failed to write ctl file: " + ex.Message);
            }
            Console.Out.WriteLine("started");
            Console.Out.Flush();
            var shouldQuit = false;
            while (!ct.IsCancellationRequested && !shouldQuit)
            {
                try
                {
                    Log("[server] SocketServerLoop: waiting for connection");
                    var client = await listener.AcceptAsync(ct).ConfigureAwait(false);
                    Log("[server] SocketServerLoop: got connection");
                    using (client)
                    using (var stream = new NetworkStream(client, ownsSocket: false))
                    {
                        var reader = new StreamReader(stream, Encoding.UTF8);
                        try
                        {
                            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(line))
                            {
                                var (quit, _) = await ProcessLineAsync(line, stream)
                                    .ConfigureAwait(false);
                                if (quit)
                                {
                                    shouldQuit = true;
                                    break;
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (IOException) { }
                        catch (ObjectDisposedException) { }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                { /* continue */
                }
            }
        }
        finally
        {
            listener.Dispose();
            try
            {
                if (File.Exists(socketPath))
                    File.Delete(socketPath);
                if (File.Exists(CtlFilePath))
                    File.Delete(CtlFilePath);
            }
            catch { }
        }
    }

    private static async Task<(bool Quit, object? Data)> ProcessLineAsync(
        string line,
        Stream responseOut
    )
    {
        try
        {
            var req = JsonSerializer.Deserialize<JsonElement>(line);
            ApplyGlobalsFromRequest(req);
            var cmd = req.TryGetProperty("cmd", out var c) ? c.GetString() : null;
            if (string.IsNullOrEmpty(cmd))
            {
                WriteResp(responseOut, ok: false, error: "cmd required");
                return (false, null);
            }

            object? result = null;
            switch (cmd.ToLowerInvariant())
            {
                case "init":
                    result = await CmdInit(req).ConfigureAwait(false);
                    break;
                case "navigate":
                    result = await CmdNavigate(req).ConfigureAwait(false);
                    break;
                case "snapshot":
                    result = await CmdSnapshot(req).ConfigureAwait(false);
                    break;
                case "click":
                    result = await CmdClick(req).ConfigureAwait(false);
                    break;
                case "type":
                    result = await CmdType(req).ConfigureAwait(false);
                    break;
                case "page_text":
                    result = await CmdPageText(req).ConfigureAwait(false);
                    break;
                case "upload":
                    result = await CmdUpload(req).ConfigureAwait(false);
                    break;
                case "tabs":
                    result = await CmdTabs(req).ConfigureAwait(false);
                    break;
                case "quit":
                    await CloseAsync().ConfigureAwait(false);
                    WriteResp(responseOut, ok: true);
                    return (true, null);
                default:
                    WriteResp(responseOut, ok: false, error: $"unknown cmd: {cmd}");
                    return (false, null);
            }

            WriteResp(responseOut, ok: true, data: result);
            return (false, result);
        }
        catch (Exception ex)
        {
            WriteResp(responseOut, ok: false, error: ex.Message);
            return (false, null);
        }
    }

    private static void WriteResp(Stream stdout, bool ok, object? data = null, string? error = null)
    {
        var obj = new Dictionary<string, object?>
        {
            ["ok"] = ok,
            ["data"] = data,
            ["error"] = error
        };
        var json = JsonSerializer.Serialize(obj, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        stdout.Write(bytes, 0, bytes.Length);
        stdout.Flush();
    }

    private static async Task<IPage> GetPageAsync()
    {
        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_page != null && !_page.IsClosed)
                return _page;

            _pw ??= await Playwright.CreateAsync().ConfigureAwait(false);
            var opts = new BrowserTypeLaunchOptions { Headless = _headless, Timeout = 30_000 };
            var channels = string.IsNullOrWhiteSpace(_channel)
                ? (
                    OperatingSystem.IsWindows()
                        ? new[] { "msedge", "chrome", "msedge-stable", "" }
                        : new[] { "msedge", "chrome", "" }
                )
                : new[] { _channel! };

            Exception? lastEx = null;
            foreach (var ch in channels)
            {
                opts.Channel = string.IsNullOrEmpty(ch) ? null : ch;
                try
                {
                    _browser = await _pw.Chromium.LaunchAsync(opts).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    Log($"[browser] channel {ch} failed: {ex.Message}");
                }
            }
            if (_browser == null)
            {
                var msg = lastEx?.Message ?? "未知";
                throw new InvalidOperationException(
                    $"未检测到 Edge 或 Chrome 浏览器（{msg}）。请先安装系统浏览器或执行 playwright install chromium。"
                );
            }
            var ctx = await _browser
                .NewContextAsync(
                    new BrowserNewContextOptions
                    {
                        ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                        UserAgent = "ShortVideo.Claw-Browser/1.0"
                    }
                )
                .ConfigureAwait(false);
            _page = await ctx.NewPageAsync().ConfigureAwait(false);
            RefMap.Clear();
            return _page;
        }
        finally
        {
            Lock.Release();
        }
    }

    private static async Task CloseAsync()
    {
        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_page != null)
            {
                await _page.CloseAsync().ConfigureAwait(false);
                _page = null;
            }
            _browser?.CloseAsync();
            _browser = null;
            _pw?.Dispose();
            _pw = null;
            RefMap.Clear();
        }
        finally
        {
            Lock.Release();
        }
    }

    private static void ApplyGlobalsFromRequest(JsonElement req)
    {
        if (!_forceVisible)
        {
            if (req.TryGetProperty("visible", out var vis))
                _headless = !vis.GetBoolean();
            if (req.TryGetProperty("headless", out var h))
                _headless = h.GetBoolean();
        }
        if (req.TryGetProperty("channel", out var ch))
            _channel = ch.GetString();
    }

    private static async Task<object?> CmdInit(JsonElement req)
    {
        if (!_forceVisible)
        {
            var needReopen = false;
            if (req.TryGetProperty("headless", out var h))
            {
                var v = h.GetBoolean();
                if (_headless != v)
                {
                    _headless = v;
                    needReopen = true;
                }
            }
            if (req.TryGetProperty("visible", out var vis))
            {
                var newHeadless = !vis.GetBoolean();
                if (_headless != newHeadless)
                {
                    _headless = newHeadless;
                    needReopen = true;
                }
            }
            if (needReopen)
                await CloseAsync().ConfigureAwait(false);
        }
        if (req.TryGetProperty("channel", out var ch))
            _channel = ch.GetString();
        return new { headless = _headless };
    }

    private static async Task<object?> CmdNavigate(JsonElement req)
    {
        var url = req.TryGetProperty("url", out var u) ? u.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("url required");

        if (
            !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https")
        )
            throw new ArgumentException("invalid URL");

        var page = await GetPageAsync().ConfigureAwait(false);
        var resp = await page.GotoAsync(
                uri.ToString(),
                new PageGotoOptions { Timeout = 30_000, WaitUntil = WaitUntilState.Load }
            )
            .ConfigureAwait(false);
        var status = resp?.Status ?? 0;
        var (pageUrl, snapshot, refs) = await GetSnapshotDataAsync(req).ConfigureAwait(false);
        return new { url = pageUrl, status, snapshot, refs };
    }

    /// <summary>获取当前页面快照数据，供各命令自动附加到返回值。</summary>
    private static async Task<(string Url, string Snapshot, IReadOnlyList<string> Refs)> GetSnapshotDataAsync(
        JsonElement? req,
        int defaultMaxChars = 15000
    )
    {
        var maxChars =
            req.HasValue
            && req.Value.TryGetProperty("maxChars", out var m)
            && m.TryGetInt32(out var n)
                ? Math.Clamp(n, 1000, 50000)
                : defaultMaxChars;
        var page = await GetPageAsync().ConfigureAwait(false);
        RefMap.Clear();
        var (snapshot, refMap) = await AccessibilitySnapshot
            .GetSnapshotAsync(page, maxChars: maxChars)
            .ConfigureAwait(false);
        foreach (var kv in refMap)
            RefMap[kv.Key] = kv.Value;
        return (page.Url, snapshot, refMap.Keys.ToList());
    }

    private static async Task<object?> CmdSnapshot(JsonElement req)
    {
        var (url, snapshot, refs) = await GetSnapshotDataAsync(req).ConfigureAwait(false);
        return new { url, snapshot, refs };
    }

    private static async Task<object?> CmdClick(JsonElement req)
    {
        var refStr = req.TryGetProperty("ref", out var r) ? r.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(refStr))
            throw new ArgumentException("ref required");

        if (!RefMap.TryGetValue(refStr, out var entry))
            throw new InvalidOperationException($"ref {refStr} not found; run snapshot first");

        var page = await GetPageAsync().ConfigureAwait(false);
        var locator = RefResolver.GetLocator(page, entry.Role, entry.Name, entry.Nth);
        if (locator == null || await locator.CountAsync().ConfigureAwait(false) == 0)
            locator = await RefResolver
                .GetLocatorIncludingFramesAsync(page, entry.Role, entry.Name, entry.Nth)
                .ConfigureAwait(false);

        if (locator == null)
            throw new InvalidOperationException($"ref {refStr} not found in page");

        await locator
            .ScrollIntoViewIfNeededAsync(
                new LocatorScrollIntoViewIfNeededOptions { Timeout = 10_000 }
            )
            .ConfigureAwait(false);
        var force = req.TryGetProperty("force", out var f) && f.GetBoolean();
        await locator
            .ClickAsync(new LocatorClickOptions { Timeout = 15_000, Force = force })
            .ConfigureAwait(false);
        var (url, snapshot, refs) = await GetSnapshotDataAsync(req).ConfigureAwait(false);
        return new { clicked = true, url, snapshot, refs };
    }

    private static async Task<object?> CmdType(JsonElement req)
    {
        var refStr = req.TryGetProperty("ref", out var r) ? r.GetString()?.Trim() : null;
        var text = req.TryGetProperty("text", out var t) ? t.GetString() : null;
        if (string.IsNullOrEmpty(refStr) || text == null)
            throw new ArgumentException("ref and text required");

        if (!RefMap.TryGetValue(refStr, out var entry))
            throw new InvalidOperationException($"ref {refStr} not found; run snapshot first");

        var page = await GetPageAsync().ConfigureAwait(false);
        var locator = RefResolver.GetLocator(page, entry.Role, entry.Name, entry.Nth);
        if (locator == null || await locator.CountAsync().ConfigureAwait(false) == 0)
            locator = await RefResolver
                .GetLocatorIncludingFramesAsync(page, entry.Role, entry.Name, entry.Nth)
                .ConfigureAwait(false);

        if (locator == null)
            throw new InvalidOperationException($"ref {refStr} not found");

        await locator
            .FillAsync(text, new LocatorFillOptions { Timeout = 10_000 })
            .ConfigureAwait(false);
        var submit = req.TryGetProperty("submit", out var s) && s.GetBoolean();
        if (submit)
            await locator
                .PressAsync("Enter", new LocatorPressOptions { Timeout = 5000 })
                .ConfigureAwait(false);
        var (url, snapshot, refs) = await GetSnapshotDataAsync(req).ConfigureAwait(false);
        return new { typed = true, submitted = submit, url, snapshot, refs };
    }

    private static async Task<object?> CmdPageText(JsonElement req)
    {
        var maxChars =
            req.TryGetProperty("maxChars", out var m) && m.TryGetInt32(out var n)
                ? Math.Clamp(n, 1000, 200_000)
                : 50_000;
        var page = await GetPageAsync().ConfigureAwait(false);
        var text =
            await page.EvaluateAsync<string>(
                    @"() => {
            const b = document.body;
            if (!b) return '';
            let t = (b.innerText || b.textContent || '').replace(/\s+/g,' ').trim();
            return t;
        }"
                )
                .ConfigureAwait(false) ?? "";
        if (text.Length > maxChars)
            text = text[..maxChars] + $"\n\n... [truncated, {text.Length - maxChars} more chars]";
        return new { text };
    }

    private static async Task<object?> CmdUpload(JsonElement req)
    {
        var refStr = req.TryGetProperty("ref", out var r) ? r.GetString()?.Trim() : null;
        if (string.IsNullOrEmpty(refStr))
            throw new ArgumentException("ref required");

        var paths = new List<string>();
        if (req.TryGetProperty("paths", out var p) && p.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in p.EnumerateArray())
            {
                var s = item.GetString()?.Trim();
                if (!string.IsNullOrEmpty(s))
                {
                    var full = Path.GetFullPath(s);
                    if (!File.Exists(full))
                        throw new FileNotFoundException(full);
                    paths.Add(full);
                }
            }
        }
        if (paths.Count == 0)
            throw new ArgumentException("paths required (non-empty array)");

        if (!RefMap.TryGetValue(refStr, out var entry))
            throw new InvalidOperationException($"ref {refStr} not found");

        var page = await GetPageAsync().ConfigureAwait(false);
        var locator = RefResolver.GetLocator(page, entry.Role, entry.Name, entry.Nth);
        if (locator == null || await locator.CountAsync().ConfigureAwait(false) == 0)
            locator = await RefResolver
                .GetLocatorIncludingFramesAsync(page, entry.Role, entry.Name, entry.Nth)
                .ConfigureAwait(false);
        if (locator == null)
            throw new InvalidOperationException($"ref {refStr} not found");

        await locator.SetInputFilesAsync(paths.ToArray()).ConfigureAwait(false);
        var (url, snapshot, refs) = await GetSnapshotDataAsync(req).ConfigureAwait(false);
        return new { count = paths.Count, url, snapshot, refs };
    }

    private static async Task<object?> CmdTabs(JsonElement req)
    {
        var page = await GetPageAsync().ConfigureAwait(false);
        var pages = page.Context.Pages;
        var list = new List<object>();
        for (var i = 0; i < pages.Count; i++)
        {
            var p = pages[i];
            list.Add(
                new
                {
                    index = i,
                    url = p.IsClosed ? "" : p.Url,
                    title = p.IsClosed ? "(closed)" : await p.TitleAsync().ConfigureAwait(false)
                }
            );
        }

        var switchIdx =
            req.TryGetProperty("switch", out var sw) && sw.TryGetInt32(out var n) ? n : -1;
        if (switchIdx >= 0 && switchIdx < pages.Count)
        {
            _page = pages[switchIdx];
            RefMap.Clear();
            var (url, snapshot, refs) = await GetSnapshotDataAsync(req).ConfigureAwait(false);
            return new { tabs = list, switchedTo = switchIdx, url, snapshot, refs };
        }
        return new { tabs = list };
    }
}
