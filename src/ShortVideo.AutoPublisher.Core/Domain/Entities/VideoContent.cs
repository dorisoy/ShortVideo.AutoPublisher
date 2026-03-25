namespace ShortVideo.AutoPublisher.Domain.Entities;

/// <summary>
/// 视频内容实体
/// </summary>
public class VideoContent
{
    /// <summary>
    /// 视频ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 视频标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 视频描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 标签（逗号分隔）
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 视频文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 视频时长（秒）
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// 封面图片路径
    /// </summary>
    public string? CoverPath { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 获取标签数组
    /// </summary>
    public string[] GetTagsArray() =>
        string.IsNullOrWhiteSpace(Tags)
            ? []
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// 设置标签数组
    /// </summary>
    public void SetTagsArray(string[] tags) =>
        Tags = tags.Length > 0 ? string.Join(",", tags) : null;

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    public string FormattedFileSize
    {
        get
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            return FileSize switch
            {
                >= GB => $"{FileSize / (double)GB:F2} GB",
                >= MB => $"{FileSize / (double)MB:F2} MB",
                >= KB => $"{FileSize / (double)KB:F2} KB",
                _ => $"{FileSize} B"
            };
        }
    }
}
