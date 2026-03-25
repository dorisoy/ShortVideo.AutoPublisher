using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;
using Spectre.Console;

namespace ShortVideo.AutoPublisher.Cli.Commands;

/// <summary>
/// 任务管理命令
/// </summary>
public static class TaskCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("task", "发布任务管理");

        // list 子命令
        var listCommand = new Command("list", "列出所有任务");
        var statusOption = new Option<string>(new[] { "-s", "--status" }, "筛选状态 (pending/running/completed/failed)");
        var limitOption = new Option<int>(new[] { "-n", "--limit" }, () => 20, "显示数量");
        listCommand.AddOption(statusOption);
        listCommand.AddOption(limitOption);
        listCommand.SetHandler(async (status, limit) => 
            await ListTasksAsync(services, status, limit), statusOption, limitOption);
        command.AddCommand(listCommand);

        // retry 子命令
        var retryCommand = new Command("retry", "重试失败任务");
        var retryIdArg = new Argument<long>("id", "任务ID");
        retryCommand.AddArgument(retryIdArg);
        retryCommand.SetHandler(async (id) => await RetryTaskAsync(services, id), retryIdArg);
        command.AddCommand(retryCommand);

        // cancel 子命令
        var cancelCommand = new Command("cancel", "取消任务");
        var cancelIdArg = new Argument<long>("id", "任务ID");
        cancelCommand.AddArgument(cancelIdArg);
        cancelCommand.SetHandler(async (id) => await CancelTaskAsync(services, id), cancelIdArg);
        command.AddCommand(cancelCommand);

        // delete 子命令
        var deleteCommand = new Command("delete", "删除任务");
        var deleteIdArg = new Argument<long>("id", "任务ID");
        deleteCommand.AddArgument(deleteIdArg);
        deleteCommand.SetHandler(async (id) => await DeleteTaskAsync(services, id), deleteIdArg);
        command.AddCommand(deleteCommand);

        return command;
    }

    private static async Task ListTasksAsync(IServiceProvider services, string? status, int limit)
    {
        var publishService = services.GetRequiredService<PublishService>();
        
        IEnumerable<Domain.Entities.PublishTask> tasks;
        
        // 筛选状态
        if (!string.IsNullOrEmpty(status))
        {
            var targetStatus = ParseStatus(status);
            if (targetStatus.HasValue)
            {
                tasks = await publishService.GetTasksByStatusAsync(targetStatus.Value);
            }
            else
            {
                tasks = await publishService.GetAllTasksAsync();
            }
        }
        else
        {
            tasks = await publishService.GetAllTasksAsync();
        }

        tasks = tasks.OrderByDescending(t => t.CreatedAt).Take(limit);

        if (!tasks.Any())
        {
            AnsiConsole.MarkupLine("[yellow]暂无任务[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("视频");
        table.AddColumn("平台");
        table.AddColumn("状态");
        table.AddColumn("进度");
        table.AddColumn("创建时间");

        foreach (var task in tasks)
        {
            var statusColor = GetStatusColor(task.Status);
            table.AddRow(
                task.Id.ToString(),
                task.VideoTitle?.Length > 20 ? task.VideoTitle[..17] + "..." : task.VideoTitle ?? "-",
                task.Platform.GetDisplayName(),
                $"[{statusColor}]{task.Status.GetDisplayName()}[/]",
                $"{task.Progress}%",
                task.CreatedAt.ToString("MM-dd HH:mm")
            );
        }

        AnsiConsole.Write(table);
    }

    private static async Task RetryTaskAsync(IServiceProvider services, long id)
    {
        var publishService = services.GetRequiredService<PublishService>();
        
        try
        {
            await publishService.RetryTaskAsync(id);
            AnsiConsole.MarkupLine($"[green]✓ 任务已重新加入队列[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
        }
    }

    private static async Task CancelTaskAsync(IServiceProvider services, long id)
    {
        var publishService = services.GetRequiredService<PublishService>();
        
        try
        {
            await publishService.CancelTaskAsync(id);
            AnsiConsole.MarkupLine($"[green]✓ 任务已取消[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
        }
    }

    private static async Task DeleteTaskAsync(IServiceProvider services, long id)
    {
        if (!AnsiConsole.Confirm($"确定要删除任务 (ID: {id}) 吗?"))
        {
            return;
        }

        var publishService = services.GetRequiredService<PublishService>();
        
        try
        {
            await publishService.DeleteTaskAsync(id);
            AnsiConsole.MarkupLine($"[green]✓ 任务已删除[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
        }
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

    private static string GetStatusColor(PublishTaskStatus status) => status switch
    {
        PublishTaskStatus.Pending => "yellow",
        PublishTaskStatus.Running => "blue",
        PublishTaskStatus.Completed => "green",
        PublishTaskStatus.Failed => "red",
        PublishTaskStatus.Cancelled => "dim",
        _ => "white"
    };
}
