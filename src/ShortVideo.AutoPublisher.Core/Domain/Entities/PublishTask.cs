using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.Domain.Entities;

/// <summary>
/// 发布任务实体
/// </summary>
public class PublishTask
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 视频ID
    /// </summary>
    public long VideoId { get; set; }

    /// <summary>
    /// 视频标题（冗余字段，便于展示）
    /// </summary>
    public string VideoTitle { get; set; } = string.Empty;

    /// <summary>
    /// 目标平台
    /// </summary>
    public PlatformType Platform { get; set; }

    /// <summary>
    /// Cookie会话ID
    /// </summary>
    public long SessionId { get; set; }

    /// <summary>
    /// 任务状态
    /// </summary>
    public PublishTaskStatus Status { get; set; } = PublishTaskStatus.Pending;

    /// <summary>
    /// 进度（0-100）
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 发布后的视频URL
    /// </summary>
    public string? PublishedUrl { get; set; }

    /// <summary>
    /// 计划发布时间
    /// </summary>
    public DateTime? ScheduledTime { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 获取平台显示名称
    /// </summary>
    public string PlatformDisplayName => Platform.GetDisplayName();

    /// <summary>
    /// 获取状态显示名称
    /// </summary>
    public string StatusDisplayName => Status.GetDisplayName();

    /// <summary>
    /// 判断任务是否可以重试
    /// </summary>
    public bool CanRetry => Status == PublishTaskStatus.Failed || Status == PublishTaskStatus.Cancelled;

    /// <summary>
    /// 判断任务是否可以取消
    /// </summary>
    public bool CanCancel => Status == PublishTaskStatus.Pending || Status == PublishTaskStatus.Running;
}
