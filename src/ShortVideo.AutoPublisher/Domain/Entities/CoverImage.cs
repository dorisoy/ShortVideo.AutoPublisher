using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.Domain.Entities;

/// <summary>
/// 封面图片实体
/// </summary>
public class CoverImage
{
    /// <summary>
    /// 封面ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 关联视频ID
    /// </summary>
    public long VideoId { get; set; }

    /// <summary>
    /// 目标平台
    /// </summary>
    public PlatformType Platform { get; set; }

    /// <summary>
    /// 图片文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 图片宽度
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 图片高度
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 获取宽高比
    /// </summary>
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;

    /// <summary>
    /// 获取平台显示名称
    /// </summary>
    public string PlatformDisplayName => Platform.GetDisplayName();

    /// <summary>
    /// 获取推荐的封面尺寸
    /// </summary>
    public static (int Width, int Height) GetRecommendedSize(PlatformType platform) => platform switch
    {
        PlatformType.Douyin => (1080, 1920),        // 9:16
        PlatformType.Xiaohongshu => (1080, 1440),   // 3:4
        PlatformType.Baijiahao => (1920, 1080),     // 16:9
        PlatformType.WeixinChannel => (1080, 1920), // 9:16
        PlatformType.Toutiao => (1920, 1080),       // 16:9
        _ => (1920, 1080)
    };
}
