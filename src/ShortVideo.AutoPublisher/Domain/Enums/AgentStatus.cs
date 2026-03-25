namespace ShortVideo.AutoPublisher.Domain.Enums;

/// <summary>
/// AI代理状态
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// 空闲
    /// </summary>
    Idle = 0,

    /// <summary>
    /// 初始化中
    /// </summary>
    Initializing = 1,

    /// <summary>
    /// 登录中
    /// </summary>
    LoggingIn = 2,

    /// <summary>
    /// 上传中
    /// </summary>
    Uploading = 3,

    /// <summary>
    /// 设置元数据中
    /// </summary>
    SettingMetadata = 4,

    /// <summary>
    /// 设置封面中
    /// </summary>
    SettingCover = 5,

    /// <summary>
    /// 发布中
    /// </summary>
    Publishing = 6,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 7,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 8
}

/// <summary>
/// AI代理状态扩展方法
/// </summary>
public static class AgentStatusExtensions
{
    /// <summary>
    /// 获取状态中文名称
    /// </summary>
    public static string GetDisplayName(this AgentStatus status) => status switch
    {
        AgentStatus.Idle => "空闲",
        AgentStatus.Initializing => "初始化中",
        AgentStatus.LoggingIn => "登录中",
        AgentStatus.Uploading => "上传中",
        AgentStatus.SettingMetadata => "设置元数据中",
        AgentStatus.SettingCover => "设置封面中",
        AgentStatus.Publishing => "发布中",
        AgentStatus.Completed => "已完成",
        AgentStatus.Error => "错误",
        _ => status.ToString()
    };
}
