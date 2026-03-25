using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

namespace ShortVideo.AutoPublisher.Services;

/// <summary>
/// 仪表盘统计数据
/// </summary>
public class DashboardStatistics
{
    public int TotalVideos { get; set; }
    public int TodayPublished { get; set; }
    public int PendingTasks { get; set; }
    public int FailedTasks { get; set; }
    public int ConnectedAccounts { get; set; }
    public int ExpiredAccounts { get; set; }
    public Dictionary<PlatformType, bool> PlatformStatus { get; set; } = new();
}

/// <summary>
/// 统计服务
/// </summary>
public class StatisticsService
{
    private readonly VideoContentRepository _videoRepository;
    private readonly PublishTaskRepository _taskRepository;
    private readonly CookieSessionRepository _sessionRepository;

    public StatisticsService(
        VideoContentRepository videoRepository,
        PublishTaskRepository taskRepository,
        CookieSessionRepository sessionRepository)
    {
        _videoRepository = videoRepository;
        _taskRepository = taskRepository;
        _sessionRepository = sessionRepository;
    }

    /// <summary>
    /// 获取仪表盘统计数据
    /// </summary>
    public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
    {
        var statusCounts = await _taskRepository.GetStatusCountsAsync();
        var platformCounts = await _sessionRepository.GetPlatformCountsAsync();

        var stats = new DashboardStatistics
        {
            TotalVideos = await _videoRepository.GetCountAsync(),
            TodayPublished = await _taskRepository.GetTodayCompletedCountAsync(),
            PendingTasks = statusCounts.GetValueOrDefault(PublishTaskStatus.Pending, 0),
            FailedTasks = statusCounts.GetValueOrDefault(PublishTaskStatus.Failed, 0),
            ConnectedAccounts = platformCounts.Values.Sum(),
            ExpiredAccounts = await _sessionRepository.GetExpiredCountAsync()
        };

        // 平台连接状态
        foreach (PlatformType platform in Enum.GetValues<PlatformType>())
        {
            stats.PlatformStatus[platform] = platformCounts.ContainsKey(platform) && platformCounts[platform] > 0;
        }

        return stats;
    }

    /// <summary>
    /// 获取各状态任务数量
    /// </summary>
    public async Task<Dictionary<PublishTaskStatus, int>> GetTaskStatusCountsAsync()
    {
        return await _taskRepository.GetStatusCountsAsync();
    }

    /// <summary>
    /// 获取各平台账号数量
    /// </summary>
    public async Task<Dictionary<PlatformType, int>> GetPlatformAccountCountsAsync()
    {
        return await _sessionRepository.GetPlatformCountsAsync();
    }
}
