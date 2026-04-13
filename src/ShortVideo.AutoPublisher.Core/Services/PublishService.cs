using System.IO;
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

    /// <summary>
    /// 最大自动重试次数
    /// </summary>
    public int MaxAutoRetryCount { get; set; } = 3;

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

        // 订阅任务状态变更事件，实现自动重试
        _scheduler.TaskStatusChanged += OnTaskStatusChanged;
    }

    /// <summary>
    /// 任务状态变更处理（自动重试）
    /// </summary>
    private async void OnTaskStatusChanged(object? sender, TaskStatusChangedEventArgs e)
    {
        if (e.Status == PublishTaskStatus.Failed)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(e.TaskId);
                if (task != null && task.RetryCount < MaxAutoRetryCount)
                {
                    _logger.LogInformation("任务 {TaskId} 失败，将自动重试 ({Current}/{Max})", e.TaskId, task.RetryCount + 1, MaxAutoRetryCount);
                    await Task.Delay(5000); // 等待 5 秒后重试
                    await RetryTaskAsync(e.TaskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动重试任务失败: {TaskId}", e.TaskId);
            }
        }
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
    /// 创建发布任务（包含完整性验证）
    /// </summary>
    public async Task<PublishTask> CreateTaskAsync(long videoId, PlatformType platform, DateTime? scheduledTime = null)
    {
        // 1. 验证视频存在
        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video == null)
        {
            throw new InvalidOperationException("视频不存在");
        }

        // 2. 验证视频文件存在且可访问
        if (string.IsNullOrEmpty(video.FilePath))
        {
            throw new InvalidOperationException("视频文件路径为空");
        }

        if (!File.Exists(video.FilePath))
        {
            throw new InvalidOperationException($"视频文件不存在: {video.FilePath}");
        }

        // 3. 验证文件大小（最小 100KB）
        var fileInfo = new FileInfo(video.FilePath);
        if (fileInfo.Length < 100 * 1024)
        {
            throw new InvalidOperationException($"视频文件过小，可能已损坏: {fileInfo.Length} bytes");
        }

        // 4. 验证平台账号存在
        var session = await _sessionRepository.GetDefaultByPlatformAsync(platform);
        if (session == null)
        {
            throw new InvalidOperationException($"未找到 {platform.GetDisplayName()} 平台的默认账号");
        }

        // 5. 验证 Cookie 有效性
        if (!session.IsValid)
        {
            throw new InvalidOperationException($"{platform.GetDisplayName()} 平台账号 Cookie 已失效，请重新登录");
        }

        if (session.IsExpired)
        {
            _logger.LogWarning("{Platform} 平台账号 Cookie 已过期，建议重新登录", platform.GetDisplayName());
        }

        // 6. 创建任务
        var task = new PublishTask
        {
            VideoId = videoId,
            VideoTitle = video.Title,
            Platform = platform,
            SessionId = session.Id,
            ScheduledTime = scheduledTime
        };

        await _scheduler.EnqueueAsync(task);
        _logger.LogInformation("发布任务已创建: Video={VideoId}, Platform={Platform}, File={FilePath}", 
            videoId, platform, video.FilePath);
        return task;
    }

    /// <summary>
    /// 批量创建发布任务
    /// </summary>
    public async Task<List<(PlatformType Platform, bool Success, string Message)>> CreateBatchTasksAsync(
        long videoId, 
        IEnumerable<PlatformType> platforms, 
        DateTime? scheduledTime = null)
    {
        var results = new List<(PlatformType, bool, string)>();
        
        foreach (var platform in platforms)
        {
            try
            {
                await CreateTaskAsync(videoId, platform, scheduledTime);
                results.Add((platform, true, "任务创建成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建 {Platform} 发布任务失败", platform);
                results.Add((platform, false, ex.Message));
            }
        }

        return results;
    }

    /// <summary>
    /// 验证视频是否可以发布
    /// </summary>
    public async Task<(bool CanPublish, List<string> Errors)> ValidateVideoForPublishAsync(long videoId, PlatformType platform)
    {
        var errors = new List<string>();

        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video == null)
        {
            errors.Add("视频不存在");
            return (false, errors);
        }

        if (string.IsNullOrEmpty(video.FilePath) || !File.Exists(video.FilePath))
        {
            errors.Add("视频文件不存在");
        }

        if (string.IsNullOrWhiteSpace(video.Title))
        {
            errors.Add("视频标题为空");
        }

        var session = await _sessionRepository.GetDefaultByPlatformAsync(platform);
        if (session == null)
        {
            errors.Add($"未配置 {platform.GetDisplayName()} 平台账号");
        }
        else if (!session.IsValid)
        {
            errors.Add($"{platform.GetDisplayName()} 账号 Cookie 已失效");
        }

        return (errors.Count == 0, errors);
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

