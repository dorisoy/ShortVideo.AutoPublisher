using System.ComponentModel;
using ModelContextProtocol.Server;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;

namespace ShortVideo.AutoPublisher.Mcp.Tools;

/// <summary>
/// 发布相关 MCP 工具
/// </summary>
[McpServerToolType]
public class PublishTools
{
    private readonly PublishService _publishService;
    private readonly VideoContentService _videoService;

    public PublishTools(PublishService publishService, VideoContentService videoService)
    {
        _publishService = publishService;
        _videoService = videoService;
    }

    /// <summary>
    /// 发布视频到指定平台
    /// </summary>
    [McpServerTool, Description("发布视频到指定平台")]
    public async Task<object> publish_video(
        [Description("视频ID")] long video_id,
        [Description("平台代码 (douyin/xiaohongshu/baijiahao/weixin/toutiao)")] string platform,
        [Description("定时发布时间，格式: yyyy-MM-dd HH:mm（可选）")] string? scheduled_time = null)
    {
        var platformType = ParsePlatform(platform);
        if (platformType == null)
        {
            return new { success = false, error = $"无效的平台: {platform}。支持: douyin, xiaohongshu, baijiahao, weixin, toutiao" };
        }

        // 验证视频
        var video = await _videoService.GetByIdAsync(video_id);
        if (video == null)
        {
            return new { success = false, error = $"视频不存在 (ID: {video_id})" };
        }

        // 解析定时时间
        DateTime? scheduledDateTime = null;
        if (!string.IsNullOrEmpty(scheduled_time))
        {
            if (DateTime.TryParse(scheduled_time, out var dt))
            {
                scheduledDateTime = dt;
            }
            else
            {
                return new { success = false, error = $"无效的时间格式: {scheduled_time}" };
            }
        }

        try
        {
            var task = await _publishService.CreateTaskAsync(video_id, platformType.Value, scheduledDateTime);
            return new
            {
                success = true,
                task = new
                {
                    task.Id,
                    VideoId = video_id,
                    VideoTitle = video.Title,
                    Platform = platformType.Value.GetDisplayName(),
                    Status = task.Status.GetDisplayName(),
                    ScheduledTime = scheduledDateTime?.ToString("yyyy-MM-dd HH:mm")
                },
                message = "发布任务已创建，使用 scheduler_start 启动调度器开始发布"
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 获取任务列表
    /// </summary>
    [McpServerTool, Description("获取发布任务列表")]
    public async Task<object> list_tasks(
        [Description("筛选状态（可选）: pending/running/completed/failed/cancelled")] string? status = null)
    {
        IEnumerable<Domain.Entities.PublishTask> tasks;

        if (!string.IsNullOrEmpty(status))
        {
            var taskStatus = ParseStatus(status);
            if (taskStatus.HasValue)
            {
                tasks = await _publishService.GetTasksByStatusAsync(taskStatus.Value);
            }
            else
            {
                tasks = await _publishService.GetAllTasksAsync();
            }
        }
        else
        {
            tasks = await _publishService.GetAllTasksAsync();
        }

        return tasks.OrderByDescending(t => t.CreatedAt).Take(50).Select(t => new
        {
            t.Id,
            t.VideoId,
            t.VideoTitle,
            Platform = t.Platform.GetDisplayName(),
            Status = t.Status.GetDisplayName(),
            t.Progress,
            t.ErrorMessage,
            CreatedAt = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            StartedAt = t.StartedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
            CompletedAt = t.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss")
        }).ToList();
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    [McpServerTool, Description("查询指定发布任务的状态")]
    public async Task<object> get_task_status([Description("任务ID")] long task_id)
    {
        var tasks = await _publishService.GetAllTasksAsync();
        var task = tasks.FirstOrDefault(t => t.Id == task_id);

        if (task == null)
        {
            return new { success = false, error = $"任务不存在 (ID: {task_id})" };
        }

        return new
        {
            success = true,
            task = new
            {
                task.Id,
                task.VideoId,
                task.VideoTitle,
                Platform = task.Platform.GetDisplayName(),
                Status = task.Status.GetDisplayName(),
                task.Progress,
                task.ErrorMessage,
                task.PublishedUrl,
                CreatedAt = task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                StartedAt = task.StartedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                CompletedAt = task.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };
    }

    /// <summary>
    /// 重试失败任务
    /// </summary>
    [McpServerTool, Description("重试失败或已取消的发布任务")]
    public async Task<object> retry_task([Description("任务ID")] long task_id)
    {
        try
        {
            await _publishService.RetryTaskAsync(task_id);
            return new { success = true, message = "任务已加入重试队列" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    [McpServerTool, Description("取消待执行或执行中的发布任务")]
    public async Task<object> cancel_task([Description("任务ID")] long task_id)
    {
        try
        {
            await _publishService.CancelTaskAsync(task_id);
            return new { success = true, message = "任务已取消" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// 启动调度器
    /// </summary>
    [McpServerTool, Description("启动任务调度器，开始执行待发布的任务")]
    public Task<object> scheduler_start()
    {
        try
        {
            _publishService.StartScheduler();
            return Task.FromResult<object>(new { success = true, message = "调度器已启动" });
        }
        catch (Exception ex)
        {
            return Task.FromResult<object>(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// 停止调度器
    /// </summary>
    [McpServerTool, Description("停止任务调度器")]
    public async Task<object> scheduler_stop()
    {
        try
        {
            await _publishService.StopSchedulerAsync();
            return new { success = true, message = "调度器已停止" };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    private static PlatformType? ParsePlatform(string platform)
    {
        return platform.ToLower() switch
        {
            "douyin" => PlatformType.Douyin,
            "xiaohongshu" => PlatformType.Xiaohongshu,
            "baijiahao" => PlatformType.Baijiahao,
            "weixin" => PlatformType.WeixinChannel,
            "toutiao" => PlatformType.Toutiao,
            _ => null
        };
    }

    private static PublishTaskStatus? ParseStatus(string status)
    {
        return status.ToLower() switch
        {
            "pending" => PublishTaskStatus.Pending,
            "running" => PublishTaskStatus.Running,
            "completed" => PublishTaskStatus.Completed,
            "failed" => PublishTaskStatus.Failed,
            "cancelled" => PublishTaskStatus.Cancelled,
            _ => null
        };
    }
}
