namespace ShortVideo.AutoPublisher.Domain.Enums;

/// <summary>
/// 平台类型
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// 抖音
    /// </summary>
    Douyin = 1,

    /// <summary>
    /// 小红书
    /// </summary>
    Xiaohongshu = 2,

    /// <summary>
    /// 百家号
    /// </summary>
    Baijiahao = 3,

    /// <summary>
    /// 微信视频号
    /// </summary>
    WeixinChannel = 4,

    /// <summary>
    /// 今日头条
    /// </summary>
    Toutiao = 5
}

/// <summary>
/// 平台类型扩展方法
/// </summary>
public static class PlatformTypeExtensions
{
    /// <summary>
    /// 获取平台中文名称
    /// </summary>
    public static string GetDisplayName(this PlatformType platform) => platform switch
    {
        PlatformType.Douyin => "抖音",
        PlatformType.Xiaohongshu => "小红书",
        PlatformType.Baijiahao => "百家号",
        PlatformType.WeixinChannel => "视频号",
        PlatformType.Toutiao => "头条",
        _ => platform.ToString()
    };

    /// <summary>
    /// 获取平台发布页URL
    /// </summary>
    public static string GetPublishUrl(this PlatformType platform) => platform switch
    {
        PlatformType.Douyin => "https://creator.douyin.com/creator-micro/content/upload",
        PlatformType.Xiaohongshu => "https://creator.xiaohongshu.com/publish/publish",
        PlatformType.Baijiahao => "https://baijiahao.baidu.com/builder/rc/edit?type=video",
        PlatformType.WeixinChannel => "https://channels.weixin.qq.com/platform/post/create",
        PlatformType.Toutiao => "https://mp.toutiao.com/profile_v4/graphic/publish",
        _ => string.Empty
    };

    /// <summary>
    /// 获取平台登录页URL
    /// </summary>
    public static string GetLoginUrl(this PlatformType platform) => platform switch
    {
        PlatformType.Douyin => "https://creator.douyin.com/",
        PlatformType.Xiaohongshu => "https://creator.xiaohongshu.com/login",
        PlatformType.Baijiahao => "https://baijiahao.baidu.com/",
        PlatformType.WeixinChannel => "https://channels.weixin.qq.com/login",
        PlatformType.Toutiao => "https://mp.toutiao.com/",
        _ => string.Empty
    };
}

/// <summary>
/// 发布任务状态
/// </summary>
public enum PublishTaskStatus
{
    /// <summary>
    /// 待发布
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 发布中
    /// </summary>
    Running = 1,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消
    /// </summary>
    Cancelled = 4
}

/// <summary>
/// 任务状态扩展方法
/// </summary>
public static class PublishTaskStatusExtensions
{
    /// <summary>
    /// 获取状态显示名称
    /// </summary>
    public static string GetDisplayName(this PublishTaskStatus status) => status switch
    {
        PublishTaskStatus.Pending => "待发布",
        PublishTaskStatus.Running => "发布中",
        PublishTaskStatus.Completed => "已完成",
        PublishTaskStatus.Failed => "失败",
        PublishTaskStatus.Cancelled => "已取消",
        _ => status.ToString()
    };
}
