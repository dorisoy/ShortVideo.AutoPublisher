using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Polly;
using Polly.Retry;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Browser;

namespace ShortVideo.AutoPublisher.OpenClaw.Agents;

/// <summary>
/// AI代理基类
/// </summary>
public abstract class AgentBase : IAiAgent
{
    protected readonly BrowserManager BrowserManager;
    protected readonly AppSettings Settings;
    protected readonly ILogger Logger;
    protected readonly AsyncRetryPolicy RetryPolicy;

    protected IBrowserContext? Context;
    protected IPage? Page;
    private AgentStatus _status = AgentStatus.Idle;
    private bool _disposed;

    public abstract PlatformType Platform { get; }
    public abstract string Name { get; }

    public AgentStatus Status
    {
        get => _status;
        protected set
        {
            if (_status != value)
            {
                var oldStatus = _status;
                _status = value;
                StatusChanged?.Invoke(this, new AgentStatusChangedEventArgs(oldStatus, value));
            }
        }
    }

    public event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<AgentProgressEventArgs>? ProgressChanged;

    protected AgentBase(BrowserManager browserManager, AppSettings settings, ILogger logger)
    {
        BrowserManager = browserManager;
        Settings = settings;
        Logger = logger;

        // 构建重试策略
        RetryPolicy = Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException))
            .WaitAndRetryAsync(
                Settings.Agent.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * Settings.Agent.RetryDelaySeconds),
                (exception, timeSpan, retryCount, context) =>
                {
                    Logger.LogWarning(exception, "操作失败，第 {RetryCount} 次重试，等待 {Delay}s", retryCount, timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// 报告进度
    /// </summary>
    protected void ReportProgress(int progress, string stage, string? message = null)
    {
        ProgressChanged?.Invoke(this, new AgentProgressEventArgs(progress, stage, message));
    }

    /// <summary>
    /// 初始化代理
    /// </summary>
    public virtual async Task InitializeAsync(CancellationToken ct = default)
    {
        Status = AgentStatus.Initializing;
        ReportProgress(0, "初始化", "正在启动浏览器...");

        try
        {
            Context = await BrowserManager.CreateContextAsync();
            Page = await BrowserManager.CreatePageAsync(Context);

            Logger.LogInformation("{Agent} 初始化完成", Name);
            Status = AgentStatus.Idle;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Agent} 初始化失败", Name);
            Status = AgentStatus.Error;
            throw;
        }
    }

    /// <summary>
    /// 登录验证
    /// </summary>
    public virtual async Task<bool> LoginAsync(CookieSession session, CancellationToken ct = default)
    {
        if (Context == null || Page == null)
        {
            await InitializeAsync(ct);
        }

        Status = AgentStatus.LoggingIn;
        ReportProgress(10, "登录", "正在加载Cookie...");

        try
        {
            // 加载Cookie
            await BrowserManager.LoadCookiesAsync(Context!, session.CookieData);

            // 导航到平台
            var url = Platform.GetPublishUrl();
            await Page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await BrowserManager.RandomDelayAsync(ct);

            // 验证登录状态
            var isLoggedIn = await CheckLoginStatusAsync(ct);
            if (!isLoggedIn)
            {
                Logger.LogWarning("{Agent} Cookie已失效", Name);
                Status = AgentStatus.Error;
                return false;
            }

            Logger.LogInformation("{Agent} 登录成功", Name);
            ReportProgress(20, "登录", "登录成功");
            Status = AgentStatus.Idle;
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Agent} 登录失败", Name);
            Status = AgentStatus.Error;
            return false;
        }
    }

    /// <summary>
    /// 检查登录状态 - 子类实现
    /// </summary>
    protected abstract Task<bool> CheckLoginStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// 上传视频
    /// </summary>
    public virtual async Task<bool> UploadVideoAsync(VideoContent video, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (Page == null) throw new InvalidOperationException("代理未初始化");

        Status = AgentStatus.Uploading;
        ReportProgress(30, "上传", "正在上传视频...");

        try
        {
            var result = await RetryPolicy.ExecuteAsync(async () =>
            {
                return await DoUploadVideoAsync(video, progress, ct);
            });

            if (result)
            {
                Logger.LogInformation("{Agent} 视频上传成功: {Title}", Name, video.Title);
                ReportProgress(60, "上传", "视频上传完成");
            }

            Status = result ? AgentStatus.Idle : AgentStatus.Error;
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Agent} 视频上传失败", Name);
            Status = AgentStatus.Error;
            return false;
        }
    }

    /// <summary>
    /// 执行视频上传 - 子类实现
    /// </summary>
    protected abstract Task<bool> DoUploadVideoAsync(VideoContent video, IProgress<int>? progress, CancellationToken ct);

    /// <summary>
    /// 设置视频元数据
    /// </summary>
    public virtual async Task<bool> SetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct = default)
    {
        if (Page == null) throw new InvalidOperationException("代理未初始化");

        Status = AgentStatus.SettingMetadata;
        ReportProgress(70, "设置元数据", "正在填写视频信息...");

        try
        {
            var result = await DoSetVideoMetadataAsync(title, tags, description, ct);
            
            if (result)
            {
                Logger.LogInformation("{Agent} 元数据设置成功", Name);
                ReportProgress(80, "设置元数据", "视频信息填写完成");
            }

            Status = result ? AgentStatus.Idle : AgentStatus.Error;
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Agent} 元数据设置失败", Name);
            Status = AgentStatus.Error;
            return false;
        }
    }

    /// <summary>
    /// 执行设置元数据 - 子类实现
    /// </summary>
    protected abstract Task<bool> DoSetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct);

    /// <summary>
    /// 设置封面图片
    /// </summary>
    public virtual async Task<bool> SetCoverImageAsync(CoverImage cover, CancellationToken ct = default)
    {
        if (Page == null) throw new InvalidOperationException("代理未初始化");

        Status = AgentStatus.SettingCover;
        ReportProgress(85, "设置封面", "正在上传封面...");

        try
        {
            var result = await DoSetCoverImageAsync(cover, ct);
            
            if (result)
            {
                Logger.LogInformation("{Agent} 封面设置成功", Name);
                ReportProgress(90, "设置封面", "封面上传完成");
            }

            Status = result ? AgentStatus.Idle : AgentStatus.Error;
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Agent} 封面设置失败", Name);
            Status = AgentStatus.Error;
            return false;
        }
    }

    /// <summary>
    /// 执行设置封面 - 子类实现
    /// </summary>
    protected abstract Task<bool> DoSetCoverImageAsync(CoverImage cover, CancellationToken ct);

    /// <summary>
    /// 发布视频
    /// </summary>
    public virtual async Task<PublishResult> PublishAsync(CancellationToken ct = default)
    {
        if (Page == null) throw new InvalidOperationException("代理未初始化");

        Status = AgentStatus.Publishing;
        ReportProgress(95, "发布", "正在发布视频...");

        try
        {
            var result = await DoPublishAsync(ct);
            
            if (result.Success)
            {
                Logger.LogInformation("{Agent} 发布成功: {Url}", Name, result.PublishedUrl);
                ReportProgress(100, "发布", "发布成功");
                Status = AgentStatus.Completed;
            }
            else
            {
                Logger.LogWarning("{Agent} 发布失败: {Error}", Name, result.ErrorMessage);
                result.ScreenshotPath = await BrowserManager.TakeScreenshotAsync(Page, $"{Platform}_error");
                Status = AgentStatus.Error;
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Agent} 发布失败", Name);
            var screenshotPath = await BrowserManager.TakeScreenshotAsync(Page, $"{Platform}_error");
            Status = AgentStatus.Error;
            
            return new PublishResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ScreenshotPath = screenshotPath
            };
        }
    }

    /// <summary>
    /// 执行发布 - 子类实现
    /// </summary>
    protected abstract Task<PublishResult> DoPublishAsync(CancellationToken ct);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Page?.CloseAsync().GetAwaiter().GetResult();
            Context?.CloseAsync().GetAwaiter().GetResult();
        }

        _disposed = true;
    }
}
