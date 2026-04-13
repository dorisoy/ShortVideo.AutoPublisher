using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ShortVideo.AutoPublisher.Core.Configuration;

namespace ShortVideo.AutoPublisher.OpenClaw.Browser;

/// <summary>
/// 浏览器管理器 - 封装 Playwright 浏览器操作
/// </summary>
public class BrowserManager : IAsyncDisposable
{
    private readonly AppSettings _settings;
    private readonly ILogger<BrowserManager> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly Random _random = new();

    public BrowserManager(AppSettings settings, ILogger<BrowserManager> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// 确保浏览器已启动
    /// </summary>
    private async Task EnsureBrowserAsync()
    {
        if (_browser is { IsConnected: true }) return;

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _settings.Browser.Headless,
            Args =
            [
                "--disable-blink-features=AutomationControlled",
                "--disable-infobars",
                "--no-sandbox"
            ]
        });
        _logger.LogInformation("浏览器已启动");
    }

    /// <summary>
    /// 创建带 Cookie 的浏览器上下文
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync(string cookieJson)
    {
        await EnsureBrowserAsync();

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            Locale = "zh-CN",
            TimezoneId = "Asia/Shanghai",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        });

        // 注入反检测脚本
        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
            Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh', 'en'] });
            window.chrome = { runtime: {} };
        ");

        // 恢复 Cookie
        if (!string.IsNullOrEmpty(cookieJson))
        {
            try
            {
                var cookies = JsonSerializer.Deserialize<List<Cookie>>(cookieJson);
                if (cookies is { Count: > 0 })
                {
                    await context.AddCookiesAsync(cookies);
                    _logger.LogDebug("已恢复 {Count} 个 Cookie", cookies.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cookie 反序列化失败");
            }
        }

        return context;
    }

    /// <summary>
    /// 随机延迟（模拟人类操作）
    /// </summary>
    public async Task RandomDelayAsync(CancellationToken ct = default)
    {
        var delay = _random.Next(_settings.Browser.MinDelayMs, _settings.Browser.MaxDelayMs);
        await Task.Delay(delay, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            try { await _browser.CloseAsync(); } catch { }
        }
        _playwright?.Dispose();
        _logger.LogInformation("浏览器已关闭");
    }
}
