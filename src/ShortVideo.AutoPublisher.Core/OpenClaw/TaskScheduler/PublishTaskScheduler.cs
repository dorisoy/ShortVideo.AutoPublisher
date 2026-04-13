using Microsoft.Extensions.Logging;
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
    public long TaskId { get; set; }
    public PublishTaskStatus Status { get; set; }

    public TaskStatusChangedEventArgs(long taskId, PublishTaskStatus status)
    {
        TaskId = taskId;
        Status = status;
    }
}

/// <summary>
/// 发布任务调度器
/// </summary>
public class PublishTaskScheduler
{
    private readonly PublishTaskRepository _taskRepository;
    private readonly CookieSessionRepository _sessionRepository;
    private readonly IAgentFactory _agentFactory;
    private readonly ILogger<PublishTaskScheduler> _logger;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    public event EventHandler<TaskStatusChangedEventArgs>? TaskStatusChanged;

    public PublishTaskScheduler(
        PublishTaskRepository taskRepository,
        CookieSessionRepository sessionRepository,
        IAgentFactory agentFactory,
        ILogger<PublishTaskScheduler> logger)
    {
        _taskRepository = taskRepository;
        _sessionRepository = sessionRepository;
        _agentFactory = agentFactory;
        _logger = logger;
    }

    /// <summary>
    /// 启动调度器
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;
        _cts = new CancellationTokenSource();
        _runningTask = RunAsync(_cts.Token);
        _logger.LogInformation("发布任务调度器已启动");
    }

    /// <summary>
    /// 停止调度器
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts == null) return;
        _cts.Cancel();
        if (_runningTask != null)
        {
            try { await _runningTask; } catch (OperationCanceledException) { }
        }
        _cts = null;
        _logger.LogInformation("发布任务调度器已停止");
    }

    /// <summary>
    /// 入队任务
    /// </summary>
    public async Task EnqueueAsync(PublishTask task)
    {
        await _taskRepository.AddAsync(task);
        _logger.LogInformation("任务已入队: {TaskId}", task.Id);
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    public async Task CancelTaskAsync(long taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task != null)
        {
            task.Status = PublishTaskStatus.Cancelled;
            await _taskRepository.UpdateAsync(task);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(taskId, PublishTaskStatus.Cancelled));
        }
    }

    /// <summary>
    /// 重试任务
    /// </summary>
    public async Task RetryTaskAsync(long taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task != null)
        {
            task.Status = PublishTaskStatus.Pending;
            task.RetryCount++;
            task.ErrorMessage = null;
            await _taskRepository.UpdateAsync(task);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(taskId, PublishTaskStatus.Pending));
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var pendingTasks = await _taskRepository.GetByStatusAsync(PublishTaskStatus.Pending);
                foreach (var task in pendingTasks)
                {
                    if (ct.IsCancellationRequested) break;

                    // 检查是否有定时发布
                    if (task.ScheduledTime.HasValue && task.ScheduledTime > DateTime.Now)
                        continue;

                    await ExecuteTaskAsync(task, ct);
                }

                await Task.Delay(5000, ct); // 每 5 秒检查一次
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调度器运行异常");
                await Task.Delay(10000, ct);
            }
        }
    }

    private async Task ExecuteTaskAsync(PublishTask task, CancellationToken ct)
    {
        IAiAgent? agent = null;
        try
        {
            task.Status = PublishTaskStatus.Running;
            task.StartedAt = DateTime.Now;
            await _taskRepository.UpdateAsync(task);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, PublishTaskStatus.Running));

            // 获取账号 Cookie
            var session = await _sessionRepository.GetByIdAsync(task.SessionId);
            if (session?.CookieData == null)
            {
                throw new InvalidOperationException("账号 Cookie 不可用");
            }

            // 创建代理并执行
            agent = _agentFactory.Create(task.Platform);
            var initialized = await agent.InitializeAsync(session.CookieData, ct);
            if (!initialized)
            {
                throw new InvalidOperationException("代理初始化失败");
            }

            var result = await agent.PublishAsync(ct);
            
            task.Status = result.Success ? PublishTaskStatus.Completed : PublishTaskStatus.Failed;
            task.ErrorMessage = result.ErrorMessage;
            task.PublishedUrl = result.PublishedUrl;
            task.CompletedAt = DateTime.Now;
            task.Progress = result.Success ? 100 : task.Progress;
            await _taskRepository.UpdateAsync(task);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, task.Status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行任务失败: {TaskId}", task.Id);
            task.Status = PublishTaskStatus.Failed;
            task.ErrorMessage = ex.Message;
            await _taskRepository.UpdateAsync(task);
            TaskStatusChanged?.Invoke(this, new TaskStatusChangedEventArgs(task.Id, PublishTaskStatus.Failed));
        }
        finally
        {
            agent?.Dispose();
        }
    }
}
