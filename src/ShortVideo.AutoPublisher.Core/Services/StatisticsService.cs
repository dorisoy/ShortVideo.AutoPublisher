using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

namespace ShortVideo.AutoPublisher.Services;

/// <summary>
/// 仪表盘统计数据
/// </summary>
public class DashboardStatistics
{
    /// <summary>视频总数</summary>
    public int TotalVideos { get; set; }

    /// <summary>今日已发布数</summary>
    public int TodayPublished { get; set; }

    /// <summary>待发布任务数</summary>
    public int PendingTasks { get; set; }

    /// <summary>失败任务数</summary>
    public int FailedTasks { get; set; }

    /// <summary>已连接账号数</summary>
    public int ConnectedAccounts { get; set; }
}

/// <summary>
/// 统计服务
/// </summary>
public class StatisticsService
{
    private readonly VideoContentRepository _videoRepository;
    private readonly PublishTaskRepository _taskRepository;
    private readonly CookieSessionRepository _sessionRepository;
    private readonly ILogger<StatisticsService> _logger;

    /// <summary>
    /// 初始化统计服务
    /// </summary>
    public StatisticsService(
        VideoContentRepository videoRepository,
        PublishTaskRepository taskRepository,
        CookieSessionRepository sessionRepository,
        ILogger<StatisticsService> logger)
    {
        _videoRepository = videoRepository;
        _taskRepository = taskRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取仪表盘统计数据
    /// </summary>
    public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
    {
        try
        {
            var videos = await _videoRepository.GetAllAsync();
            var sessions = await _sessionRepository.GetAllAsync();
            var pendingTasks = await _taskRepository.GetByStatusAsync(PublishTaskStatus.Pending);
            var failedTasks = await _taskRepository.GetByStatusAsync(PublishTaskStatus.Failed);
            var completedToday = await _taskRepository.GetRecentTasksAsync(1);
            var todayPublished = completedToday.Count(t =>
                t.Status == PublishTaskStatus.Completed &&
                t.CompletedAt?.Date == DateTime.Today);

            return new DashboardStatistics
            {
                TotalVideos = videos.Count(),
                TodayPublished = todayPublished,
                PendingTasks = pendingTasks.Count(),
                FailedTasks = failedTasks.Count(),
                ConnectedAccounts = sessions.Count(s => s.IsValid)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仪表盘统计数据失败");
            return new DashboardStatistics();
        }
    }

    /// <summary>
    /// 获取任务状态计数
    /// </summary>
    public async Task<Dictionary<PublishTaskStatus, int>> GetTaskStatusCountsAsync()
    {
        var result = new Dictionary<PublishTaskStatus, int>();

        try
        {
            foreach (PublishTaskStatus status in Enum.GetValues<PublishTaskStatus>())
            {
                var tasks = await _taskRepository.GetByStatusAsync(status);
                result[status] = tasks.Count();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务状态计数失败");
        }

        return result;
    }
}
