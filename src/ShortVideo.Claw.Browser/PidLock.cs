using System.Diagnostics;
using File = System.IO.File;

namespace ShortVideo.Claw.Browser;

/// <summary>
/// Nginx 风格的 PID 文件锁：主进程持有独占文件锁并写入 PID，
/// 进程退出时 OS 自动释放锁，其他进程通过尝试获取锁判断主进程是否存活。
/// </summary>
internal sealed class PidLock : IDisposable
{
    private readonly FileStream _stream;
    public int Pid { get; }
    public string Path { get; }

    private PidLock(FileStream stream, int pid, string path)
    {
        _stream = stream;
        Pid = pid;
        Path = path;
    }

    /// <summary>
    /// 尝试获取 PID 文件锁。成功则返回持有者，失败（已有主进程）返回 null。
    /// </summary>
    public static PidLock? TryAcquire(string? baseDir = null)
    {
        baseDir ??= AppContext.BaseDirectory;
        var path = System.IO.Path.Combine(baseDir, "ShortVideo.Claw-browser.pid");
        try
        {
            // FileShare.None = 独占锁，其他进程无法同时打开
            var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 32,
                FileOptions.DeleteOnClose
            );
            var pid = Environment.ProcessId;
            var content = System.Text.Encoding.UTF8.GetBytes(pid.ToString());
            stream.SetLength(0);
            stream.Write(content, 0, content.Length);
            stream.Flush(true);
            return new PidLock(stream, pid, path);
        }
        catch (IOException)
        {
            return null; // 文件被其他进程锁定
        }
    }

    /// <summary>
    /// 检查指定 PID 的进程是否仍在运行。
    /// </summary>
    public static bool IsProcessAlive(int pid)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => _stream.Dispose();
}

/// <summary>
/// 启动锁：spawn master 时持有，防止多进程同时繁殖。文件锁实现，无 Mutex。
/// </summary>
internal sealed class StartLock : IDisposable
{
    private readonly FileStream _stream;

    private StartLock(FileStream stream) => _stream = stream;

    /// <summary>尝试获取启动锁。成功返回持有者，失败（他进程正在 spawn）返回 null。</summary>
    public static StartLock? TryAcquire(string? baseDir = null)
    {
        baseDir ??= AppContext.BaseDirectory;
        var path = System.IO.Path.Combine(baseDir, "ShortVideo.Claw-browser.starting");
        try
        {
            var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 8,
                FileOptions.DeleteOnClose
            );
            return new StartLock(stream);
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Dispose() => _stream.Dispose();
}

/// <summary>
/// 请求锁：客户端发送请求时持有，串行化请求。文件锁实现，无 Mutex。
/// </summary>
internal sealed class ReqLock : IDisposable
{
    private readonly FileStream _stream;

    private ReqLock(FileStream stream) => _stream = stream;

    /// <summary>等待并获取请求锁，超时返回 null。</summary>
    public static ReqLock? WaitAndAcquire(string? baseDir = null, int timeoutMs = 30_000)
    {
        baseDir ??= AppContext.BaseDirectory;
        var path = System.IO.Path.Combine(baseDir, "ShortVideo.Claw-browser.req");
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            try
            {
                var stream = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 8,
                    FileOptions.DeleteOnClose
                );
                return new ReqLock(stream);
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }
        return null;
    }

    public void Dispose() => _stream.Dispose();
}
