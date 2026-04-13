using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.Domain.Entities;

/// <summary>
/// 视频内容实体
/// </summary>
public class VideoContent
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public string FormattedFileSize => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{FileSize / (1024.0 * 1024):F1} MB",
        _ => $"{FileSize / (1024.0 * 1024 * 1024):F2} GB"
    };
}

/// <summary>
/// 发布任务实体
/// </summary>
public class PublishTask
{
    public long Id { get; set; }
    public long VideoId { get; set; }
    public string? VideoTitle { get; set; }
    public PlatformType Platform { get; set; }
    public long SessionId { get; set; }
    public PublishTaskStatus Status { get; set; } = PublishTaskStatus.Pending;
    public int Progress { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PublishedUrl { get; set; }
    public DateTime? ScheduledTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Cookie 会话实体
/// </summary>
public class CookieSession
{
    public long Id { get; set; }
    public PlatformType Platform { get; set; }
    public string? AccountName { get; set; }
    public string? AccountId { get; set; }
    public string? CookieData { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Cookie 是否过期（超过 7 天未验证）
    /// </summary>
    public bool IsExpired => LastValidatedAt.HasValue && (DateTime.Now - LastValidatedAt.Value).TotalDays > 7;
}

/// <summary>
/// 封面图片
/// </summary>
public class CoverImage
{
    public string FilePath { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}
