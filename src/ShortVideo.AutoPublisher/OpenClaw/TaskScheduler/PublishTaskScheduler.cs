using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;

namespace ShortVideo.AutoPublisher.OpenClaw.TaskScheduler;

/// <summary>
/// 任务状态变更事件参数
/// </summary>
public class TaskStatusChangedEventArgs : EventArgs
{
    public long TaskId { get; }
    public PublishTaskStatus Status { get; }
    public int Progress { get; }
    public string? Message { get; }

    public TaskStatusChangedEventArgs(long taskId, PublishTaskStatus status, int progress = 0, string? message = null)
    {
        TaskId = taskId;
        Status = status;
        Progress = progress;
        Message = message;
    }
}

/// <summary>
/// 发布任务调度器
/// </summary>
public class PublishTaskScheduler : IDisposable
{
    private readonly ILogger<PublishTaskScheduler> _logger;
    private readonly AppSettings _settings;
    private readonly PublishTaskRepository _taskRepository;
    private readonly VideoContentRepository _videoRepository;
    private readonly CookieSessionRepository _sessionRepository;
    private readonly IAgentFactory _agentFactory;

    private readonly ConcurrentQueue<PublishTask> _taskQueue = new();
    private readonly ConcurrentDictionary<long, CancellationTokenSource> _runningTasks = new();
    private readonly SemaphoreSlim _semaphore;
    private CancellationTokenSource? _schedulerCts;
    private Task? _schedulerTask;
    private bool _disposed;

    /// <summary>
    /// 任务状态变更事件
    /// </summary>
    public event EventHandler<TaskStatusChangedEventArgs>? TaskStatusChanged;

    /// <summary>
    /// 调度器是否正在运行
    /// </summary>
    public bool IsRunning => _schedulerTask != null && !_schedulerTask.IsCompleted;

    public PublishTaskScheduler(
        AppSettings settings,
        PublishTaskRepository taskRepository,
        VideoContentRepository videoRepository,
        CookieSessionRepository sessionRepository,
        IAgentFactory agentFactory,
        ILogger<PublishTaskScheduler> logger)
    {
        _settings = settings;
        _taskRepository = taskRepository;
        _videoRepository = videoRepository;
        _sessionRepository = sessionRepository;
        _agentFactory = agentFactory;
        _logger = logger;
        _semaphore = new SemaphoreSlim(settings.Agent.MaxConcurrentTasks);
    }

    /// <summary>
    /// 启动调度器
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;

        _logger.LogInformation("启动任务调度器...");
        _schedulerCts = new CancellationTokenSource();
        _schedulerTask = RunSchedulerAsync(_schedulerCts.Token);
    }

    /// <summary>
    /// 停止调度器
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning) return;

        _logger.LogInformation("停止任务调度器...");
        _schedulerCts?.Cancel();

        foreach (var cts in _runningTasks.Values)
        {
            cts.Cancel();
        }

        if (_schedulerTask != null)
        {
            await _schedulerTask;
        }

        _logger.LogInformation("任务调度器已停止");
    }

    /// <summary>
    /// 添加任务到队列
    /// </summary>
    public async Task<long> EnqueueAsync(PublishTask task)
    {
        var taskId = await _taskRepository.AddAsync(task);
        _taskQueue.Enqueue(task);
        _logger.LogInformation("任务已加入队列: {TaskId}", taskId);
        return taskId;
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    public async Task CancelTaskAsync(long taskId)
    {
        if (_runningTasks.TryRemove(taskId, out var cts))
        {
            cts.Cancel();
        }

        await _taskRepository.UpdateStatusAsync(taskId, PublishTaskStatus.Cancelled);
        TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(taskId, PublishTaskStatus.Cancelled));
        _logger.LogInformation("任务已取消: {TaskId}", taskId);
    }

    /// <summary>
    /// 重试任务
    /// </summary>
    public async Task RetryTaskAsync(long taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || !task.CanRetry) return;

        await _taskRepository.IncrementRetryCountAsync(taskId);
        task.Status = PublishTaskStatus.Pending;
        _taskQueue.Enqueue(task);

        _logger.LogInformation("任务已加入重试队列: {TaskId}", taskId);
    }

    private async Task RunSchedulerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 从数据库加载待执行任务
                var pendingTasks = await _taskRepository.GetPendingTasksAsync();
                foreach (var task in pendingTasks)
                {
                    if (!_taskQueue.Any(t => t.Id == task.Id))
                    {
                        _taskQueue.Enqueue(task);
                    }
                }

                // 处理队列中的任务
                while (_taskQueue.TryDequeue(out var task))
                {
                    await _semaphore.WaitAsync(ct);
                    _ = ExecuteTaskAsync(task, ct);
                }

                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调度器运行错误");
                await Task.Delay(5000, ct);
            }
        }
    }

    private async Task ExecuteTaskAsync(PublishTask task, CancellationToken schedulerCt)
    {
        var taskCts = CancellationTokenSource.CreateLinkedTokenSource(schedulerCt);
        _runningTasks[task.Id] = taskCts;

        try
        {
            _logger.LogInformation("开始执行任务: {TaskId}", task.Id);
            await _taskRepository.UpdateStatusAsync(task.Id, PublishTaskStatus.Running);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, PublishTaskStatus.Running));

            // 获取视频和会话信息
            var video = await _videoRepository.GetByIdAsync(task.VideoId);
            var session = await _sessionRepository.GetByIdAsync(task.SessionId);

            if (video == null || session == null)
            {
                throw new InvalidOperationException("视频或会话不存在");
            }

            // 创建并执行代理
            using var agent = _agentFactory.CreateAgent(task.Platform);
            
            agent.ProgressChanged += (s, e) =>
            {
                _ = _taskRepository.UpdateProgressAsync(task.Id, e.Progress);
                TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, PublishTaskStatus.Running, e.Progress, e.Message));
            };

            await agent.InitializeAsync(taskCts.Token);
            
            if (!await agent.LoginAsync(session, taskCts.Token))
            {
                throw new Exception("登录失败，Cookie可能已失效");
            }

            if (!await agent.UploadVideoAsync(video, null, taskCts.Token))
            {
                throw new Exception("视频上传失败");
            }

            if (!await agent.SetVideoMetadataAsync(video.Title, video.GetTagsArray(), video.Description ?? "", taskCts.Token))
            {
                throw new Exception("设置元数据失败");
            }

            var result = await agent.PublishAsync(taskCts.Token);
            
            if (result.Success)
            {
                await _taskRepository.UpdateStatusAsync(task.Id, PublishTaskStatus.Completed, 100);
                if (!string.IsNullOrEmpty(result.PublishedUrl))
                {
                    await _taskRepository.SetPublishedUrlAsync(task.Id, result.PublishedUrl);
                }
                TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, PublishTaskStatus.Completed, 100, "发布成功"));
                _logger.LogInformation("任务完成: {TaskId}", task.Id);
            }
            else
            {
                throw new Exception(result.ErrorMessage ?? "发布失败");
            }
        }
        catch (OperationCanceledException)
        {
            await _taskRepository.UpdateStatusAsync(task.Id, PublishTaskStatus.Cancelled);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, PublishTaskStatus.Cancelled, 0, "任务已取消"));
            _logger.LogInformation("任务已取消: {TaskId}", task.Id);
        }
        catch (Exception ex)
        {
            await _taskRepository.UpdateStatusAsync(task.Id, PublishTaskStatus.Failed, 0, ex.Message);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, PublishTaskStatus.Failed, 0, ex.Message));
            _logger.LogError(ex, "任务失败: {TaskId}", task.Id);
        }
        finally
        {
            _runningTasks.TryRemove(task.Id, out _);
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _schedulerCts?.Cancel();
            _schedulerCts?.Dispose();
            _semaphore.Dispose();
            
            foreach (var cts in _runningTasks.Values)
            {
                cts.Dispose();
            }
        }

        _disposed = true;
    }
}
