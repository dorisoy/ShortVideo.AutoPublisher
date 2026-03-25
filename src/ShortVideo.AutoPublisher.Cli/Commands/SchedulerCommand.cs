using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ShortVideo.AutoPublisher.OpenClaw.TaskScheduler;
using ShortVideo.AutoPublisher.Services;
using Spectre.Console;

namespace ShortVideo.AutoPublisher.Cli.Commands;

/// <summary>
/// 调度器命令
/// </summary>
public static class SchedulerCommand
{
    private static CancellationTokenSource? _cts;

    public static Command Create(IServiceProvider services)
    {
        var command = new Command("scheduler", "任务调度器控制");

        // start 子命令
        var startCommand = new Command("start", "启动调度器 (前台运行)");
        startCommand.SetHandler(async () => await StartSchedulerAsync(services));
        command.AddCommand(startCommand);

        // status 子命令
        var statusCommand = new Command("status", "查看调度器状态");
        statusCommand.SetHandler(() => ShowSchedulerStatus(services));
        command.AddCommand(statusCommand);

        return command;
    }

    private static async Task StartSchedulerAsync(IServiceProvider services)
    {
        var publishService = services.GetRequiredService<PublishService>();
        _cts = new CancellationTokenSource();

        AnsiConsole.MarkupLine("[green]调度器已启动[/]");
        AnsiConsole.MarkupLine("[dim]按 Ctrl+C 停止[/]");
        AnsiConsole.WriteLine();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            _cts.Cancel();
            AnsiConsole.MarkupLine("\n[yellow]正在停止调度器...[/]");
        };

        try
        {
            publishService.StartScheduler();
            
            // 主循环 - 等待取消
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        finally
        {
            await publishService.StopSchedulerAsync();
            AnsiConsole.MarkupLine("[green]调度器已停止[/]");
        }
    }

    private static void ShowSchedulerStatus(IServiceProvider services)
    {
        var scheduler = services.GetRequiredService<PublishTaskScheduler>();
        var isRunning = scheduler.IsRunning;

        var statusText = isRunning ? "[green]运行中[/]" : "[yellow]已停止[/]";
        AnsiConsole.MarkupLine($"调度器状态: {statusText}");
    }
}
