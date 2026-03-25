namespace ShortVideo.AutoPublisher.Domain.Enums;

/// <summary>
/// 发布任务状态
/// </summary>
public enum PublishTaskStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 执行中
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
/// 发布任务状态扩展方法
/// </summary>
public static class PublishTaskStatusExtensions
{
    /// <summary>
    /// 获取状态中文名称
    /// </summary>
    public static string GetDisplayName(this PublishTaskStatus status) => status switch
    {
        PublishTaskStatus.Pending => "等待中",
        PublishTaskStatus.Running => "执行中",
        PublishTaskStatus.Completed => "已完成",
        PublishTaskStatus.Failed => "失败",
        PublishTaskStatus.Cancelled => "已取消",
        _ => status.ToString()
    };

    /// <summary>
    /// 判断任务是否已结束
    /// </summary>
    public static bool IsTerminal(this PublishTaskStatus status) =>
        status is PublishTaskStatus.Completed or PublishTaskStatus.Failed or PublishTaskStatus.Cancelled;
}
