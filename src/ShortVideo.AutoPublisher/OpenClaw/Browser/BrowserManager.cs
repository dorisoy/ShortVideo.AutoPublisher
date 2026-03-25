using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ShortVideo.AutoPublisher.Core.Configuration;

namespace ShortVideo.AutoPublisher.OpenClaw.Browser;

/// <summary>
/// Playwright浏览器管理器
/// </summary>
public class BrowserManager : IAsyncDisposable
{
    private readonly ILogger<BrowserManager> _logger;
    private readonly AppSettings _settings;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public BrowserManager(AppSettings settings, ILogger<BrowserManager> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// 获取浏览器实例
    /// </summary>
    public async Task<IBrowser> GetBrowserAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_browser != null && _browser.IsConnected)
            {
                return _browser;
            }

            _logger.LogInformation("启动浏览器...");

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
            return _browser;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 创建新的浏览器上下文
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync(string? userDataDir = null)
    {
        var browser = await GetBrowserAsync();

        var options = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "zh-CN",
            TimezoneId = "Asia/Shanghai",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        };

        var context = await browser.NewContextAsync(options);

        // 注入反检测脚本
        if (_settings.Agent.EnableAntiDetection)
        {
            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh', 'en'] });
                window.chrome = { runtime: {} };
            ");
        }

        return context;
    }

    /// <summary>
    /// 创建新页面
    /// </summary>
    public async Task<IPage> CreatePageAsync(IBrowserContext context)
    {
        var page = await context.NewPageAsync();
        
        page.SetDefaultTimeout(_settings.Browser.ActionTimeoutSeconds * 1000);
        page.SetDefaultNavigationTimeout(_settings.Browser.PageLoadTimeoutSeconds * 1000);

        return page;
    }

    /// <summary>
    /// 加载Cookie到上下文
    /// </summary>
    public async Task LoadCookiesAsync(IBrowserContext context, string cookieJson)
    {
        try
        {
            var cookies = System.Text.Json.JsonSerializer.Deserialize<Cookie[]>(cookieJson);
            if (cookies != null && cookies.Length > 0)
            {
                await context.AddCookiesAsync(cookies);
                _logger.LogInformation("已加载 {Count} 个Cookie", cookies.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载Cookie失败");
            throw;
        }
    }

    /// <summary>
    /// 保存Cookie
    /// </summary>
    public async Task<string> SaveCookiesAsync(IBrowserContext context)
    {
        var cookies = await context.CookiesAsync();
        return System.Text.Json.JsonSerializer.Serialize(cookies);
    }

    /// <summary>
    /// 截图
    /// </summary>
    public async Task<string> TakeScreenshotAsync(IPage page, string? name = null)
    {
        var screenshotDir = Path.Combine(AppContext.BaseDirectory, "Screenshots");
        if (!Directory.Exists(screenshotDir))
        {
            Directory.CreateDirectory(screenshotDir);
        }

        var fileName = $"{name ?? "screenshot"}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(screenshotDir, fileName);

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = filePath, FullPage = true });
        _logger.LogInformation("截图已保存: {Path}", filePath);

        return filePath;
    }

    /// <summary>
    /// 随机延迟
    /// </summary>
    public async Task RandomDelayAsync(CancellationToken ct = default)
    {
        var delay = Random.Shared.Next(_settings.Browser.MinDelayMs, _settings.Browser.MaxDelayMs);
        await Task.Delay(delay, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _lock.WaitAsync();
        try
        {
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            _playwright?.Dispose();
            _playwright = null;

            _disposed = true;
            _logger.LogInformation("浏览器已关闭");
        }
        finally
        {
            _lock.Release();
        }
    }
}
