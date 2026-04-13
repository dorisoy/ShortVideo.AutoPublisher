using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Browser;

namespace ShortVideo.AutoPublisher.OpenClaw.Abstractions;

/// <summary>
/// 代理状态
/// </summary>
public enum AgentStatus
{
    Idle,
    Initializing,
    Ready,
    Working,
    Error,
    Disposed
}

/// <summary>
/// 发布结果
/// </summary>
public class PublishResult
{
    public bool Success { get; set; }
    public string? PublishedUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ScreenshotPath { get; set; }
}

/// <summary>
/// 代理状态变更事件参数
/// </summary>
public class AgentStatusChangedEventArgs : EventArgs
{
    public AgentStatus OldStatus { get; set; }
    public AgentStatus NewStatus { get; set; }
    public string? Message { get; set; }

    public AgentStatusChangedEventArgs(AgentStatus oldStatus, AgentStatus newStatus, string? message = null)
    {
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Message = message;
    }
}

/// <summary>
/// 代理进度事件参数
/// </summary>
public class AgentProgressEventArgs : EventArgs
{
    public int Progress { get; set; }
    public string Stage { get; set; }
    public string? Message { get; set; }

    public AgentProgressEventArgs(int progress, string stage, string? message = null)
    {
        Progress = progress;
        Stage = stage;
        Message = message;
    }
}

/// <summary>
/// AI 代理接口
/// </summary>
public interface IAiAgent : IDisposable
{
    PlatformType Platform { get; }
    string Name { get; }
    AgentStatus Status { get; }
    event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    event EventHandler<AgentProgressEventArgs>? ProgressChanged;
    Task<bool> InitializeAsync(string cookieData, CancellationToken ct = default);
    Task<bool> UploadVideoAsync(VideoContent video, IProgress<int>? progress = null, CancellationToken ct = default);
    Task<bool> SetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct = default);
    Task<bool> SetCoverImageAsync(CoverImage cover, CancellationToken ct = default);
    Task<PublishResult> PublishAsync(CancellationToken ct = default);
}

/// <summary>
/// 代理工厂接口
/// </summary>
public interface IAgentFactory
{
    IAiAgent Create(PlatformType platform);
}

/// <summary>
/// 代理基类
/// </summary>
public abstract class AgentBase : IAiAgent
{
    public BrowserManager BrowserManager { get; }
    public AppSettings Settings { get; }
    public ILogger Logger { get; }
    public IBrowserContext? Context { get; protected set; }
    public IPage? Page { get; protected set; }
    public abstract PlatformType Platform { get; }
    public abstract string Name { get; }
    public AgentStatus Status { get; protected set; } = AgentStatus.Idle;
    
    public event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<AgentProgressEventArgs>? ProgressChanged;

    protected AgentBase(BrowserManager browserManager, AppSettings settings, ILogger logger)
    {
        BrowserManager = browserManager;
        Settings = settings;
        Logger = logger;
    }

    protected void SetStatus(AgentStatus newStatus, string? message = null)
    {
        var old = Status;
        Status = newStatus;
        StatusChanged?.Invoke(this, new AgentStatusChangedEventArgs(old, newStatus, message));
    }

    protected void ReportProgress(int progress, string stage, string? message = null)
    {
        ProgressChanged?.Invoke(this, new AgentProgressEventArgs(progress, stage, message));
    }

    public virtual async Task<bool> InitializeAsync(string cookieData, CancellationToken ct = default)
    {
        try
        {
            SetStatus(AgentStatus.Initializing);
            Context = await BrowserManager.CreateContextAsync(cookieData);
            Page = await Context.NewPageAsync();
            
            var publishUrl = Platform.GetPublishUrl();
            await Page.GotoAsync(publishUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            
            var isLoggedIn = await CheckLoginStatusAsync(ct);
            if (!isLoggedIn)
            {
                SetStatus(AgentStatus.Error, "登录状态验证失败");
                return false;
            }

            SetStatus(AgentStatus.Ready);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "初始化 {Name} 失败", Name);
            SetStatus(AgentStatus.Error, ex.Message);
            return false;
        }
    }

    public async Task<bool> UploadVideoAsync(VideoContent video, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        SetStatus(AgentStatus.Working, "正在上传视频");
        return await DoUploadVideoAsync(video, progress, ct);
    }

    public async Task<bool> SetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct = default)
    {
        return await DoSetVideoMetadataAsync(title, tags, description, ct);
    }

    public async Task<bool> SetCoverImageAsync(CoverImage cover, CancellationToken ct = default)
    {
        return await DoSetCoverImageAsync(cover, ct);
    }

    public async Task<PublishResult> PublishAsync(CancellationToken ct = default)
    {
        SetStatus(AgentStatus.Working, "正在发布");
        var result = await DoPublishAsync(ct);
        SetStatus(result.Success ? AgentStatus.Idle : AgentStatus.Error, result.ErrorMessage);
        return result;
    }

    protected abstract Task<bool> CheckLoginStatusAsync(CancellationToken ct = default);
    protected abstract Task<bool> DoUploadVideoAsync(VideoContent video, IProgress<int>? progress, CancellationToken ct);
    protected abstract Task<bool> DoSetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct);
    protected abstract Task<bool> DoSetCoverImageAsync(CoverImage cover, CancellationToken ct);
    protected abstract Task<PublishResult> DoPublishAsync(CancellationToken ct);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Context?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            SetStatus(AgentStatus.Disposed);
        }
    }
}
