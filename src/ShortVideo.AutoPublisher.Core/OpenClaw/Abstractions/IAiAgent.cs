using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.OpenClaw.Abstractions;

/// <summary>
/// 代理状态变更事件参数
/// </summary>
public class AgentStatusChangedEventArgs : EventArgs
{
    public AgentStatus OldStatus { get; }
    public AgentStatus NewStatus { get; }
    public string? Message { get; }

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
    public int Progress { get; }
    public string Stage { get; }
    public string? Message { get; }

    public AgentProgressEventArgs(int progress, string stage, string? message = null)
    {
        Progress = progress;
        Stage = stage;
        Message = message;
    }
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
/// AI代理接口
/// </summary>
public interface IAiAgent : IDisposable
{
    /// <summary>
    /// 平台类型
    /// </summary>
    PlatformType Platform { get; }

    /// <summary>
    /// 代理名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    AgentStatus Status { get; }

    /// <summary>
    /// 状态变更事件
    /// </summary>
    event EventHandler<AgentStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// 进度变更事件
    /// </summary>
    event EventHandler<AgentProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// 初始化代理
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// 登录验证
    /// </summary>
    Task<bool> LoginAsync(CookieSession session, CancellationToken ct = default);

    /// <summary>
    /// 上传视频
    /// </summary>
    Task<bool> UploadVideoAsync(VideoContent video, IProgress<int>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// 设置视频元数据
    /// </summary>
    Task<bool> SetVideoMetadataAsync(string title, string[] tags, string description, CancellationToken ct = default);

    /// <summary>
    /// 设置封面图片
    /// </summary>
    Task<bool> SetCoverImageAsync(CoverImage cover, CancellationToken ct = default);

    /// <summary>
    /// 发布视频
    /// </summary>
    Task<PublishResult> PublishAsync(CancellationToken ct = default);
}
