using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;
using ShortVideo.AutoPublisher.OpenClaw.TaskScheduler;

namespace ShortVideo.AutoPublisher.Services;

/// <summary>
/// 发布服务
/// </summary>
public class PublishService
{
    private readonly PublishTaskRepository _taskRepository;
    private readonly VideoContentRepository _videoRepository;
    private readonly CookieSessionRepository _sessionRepository;
    private readonly PublishTaskScheduler _scheduler;
    private readonly ILogger<PublishService> _logger;

    public PublishService(
        PublishTaskRepository taskRepository,
        VideoContentRepository videoRepository,
        CookieSessionRepository sessionRepository,
        PublishTaskScheduler scheduler,
        ILogger<PublishService> logger)
    {
        _taskRepository = taskRepository;
        _videoRepository = videoRepository;
        _sessionRepository = sessionRepository;
        _scheduler = scheduler;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有任务
    /// </summary>
    public async Task<IEnumerable<PublishTask>> GetAllTasksAsync()
    {
        return await _taskRepository.GetAllAsync();
    }

    /// <summary>
    /// 根据状态获取任务
    /// </summary>
    public async Task<IEnumerable<PublishTask>> GetTasksByStatusAsync(PublishTaskStatus status)
    {
        return await _taskRepository.GetByStatusAsync(status);
    }

    /// <summary>
    /// 获取最近任务
    /// </summary>
    public async Task<IEnumerable<PublishTask>> GetRecentTasksAsync(int count = 5)
    {
        return await _taskRepository.GetRecentTasksAsync(count);
    }

    /// <summary>
    /// 创建发布任务
    /// </summary>
    public async Task<PublishTask> CreateTaskAsync(long videoId, PlatformType platform, DateTime? scheduledTime = null)
    {
        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video == null)
        {
            throw new InvalidOperationException("视频不存在");
        }

        var session = await _sessionRepository.GetDefaultByPlatformAsync(platform);
        if (session == null)
        {
            throw new InvalidOperationException($"未找到 {platform.GetDisplayName()} 平台的默认账号");
        }

        var task = new PublishTask
        {
            VideoId = videoId,
            VideoTitle = video.Title,
            Platform = platform,
            SessionId = session.Id,
            ScheduledTime = scheduledTime
        };

        await _scheduler.EnqueueAsync(task);
        _logger.LogInformation("发布任务已创建: Video={VideoId}, Platform={Platform}", videoId, platform);
        return task;
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    public async Task CancelTaskAsync(long taskId)
    {
        await _scheduler.CancelTaskAsync(taskId);
        _logger.LogInformation("任务已取消: {TaskId}", taskId);
    }

    /// <summary>
    /// 重试任务
    /// </summary>
    public async Task RetryTaskAsync(long taskId)
    {
        await _scheduler.RetryTaskAsync(taskId);
        _logger.LogInformation("任务已重试: {TaskId}", taskId);
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    public async Task DeleteTaskAsync(long taskId)
    {
        await _taskRepository.DeleteAsync(taskId);
        _logger.LogInformation("任务已删除: {TaskId}", taskId);
    }

    /// <summary>
    /// 启动调度器
    /// </summary>
    public void StartScheduler()
    {
        _scheduler.Start();
    }

    /// <summary>
    /// 停止调度器
    /// </summary>
    public async Task StopSchedulerAsync()
    {
        await _scheduler.StopAsync();
    }
}

