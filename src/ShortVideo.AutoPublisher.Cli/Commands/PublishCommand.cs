using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;
using Spectre.Console;

namespace ShortVideo.AutoPublisher.Cli.Commands;

/// <summary>
/// 发布命令
/// </summary>
public static class PublishCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("publish", "发布视频到平台");

        var videoOption = new Option<long>(new[] { "-v", "--video" }, "视频ID") { IsRequired = true };
        var platformOption = new Option<string>(new[] { "-p", "--platform" }, "平台 (douyin/xiaohongshu/baijiahao/weixin/toutiao)");
        var allOption = new Option<bool>(new[] { "--all" }, () => false, "发布到所有已配置账号的平台");
        var scheduleOption = new Option<string>(new[] { "-s", "--schedule" }, "定时发布时间 (格式: yyyy-MM-dd HH:mm)");

        command.AddOption(videoOption);
        command.AddOption(platformOption);
        command.AddOption(allOption);
        command.AddOption(scheduleOption);

        command.SetHandler(async (videoId, platform, all, schedule) =>
            await PublishVideoAsync(services, videoId, platform, all, schedule),
            videoOption, platformOption, allOption, scheduleOption);

        return command;
    }

    private static async Task PublishVideoAsync(IServiceProvider services, long videoId, string? platform, bool all, string? schedule)
    {
        var videoService = services.GetRequiredService<VideoContentService>();
        var sessionService = services.GetRequiredService<CookieSessionService>();
        var publishService = services.GetRequiredService<PublishService>();

        // 验证视频
        var video = await videoService.GetByIdAsync(videoId);
        if (video == null)
        {
            AnsiConsole.MarkupLine($"[red]错误: 视频不存在 (ID: {videoId})[/]");
            return;
        }

        // 解析定时时间
        DateTime? scheduledTime = null;
        if (!string.IsNullOrEmpty(schedule))
        {
            if (DateTime.TryParse(schedule, out var dt))
            {
                scheduledTime = dt;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]错误: 无效的时间格式 - {schedule}[/]");
                return;
            }
        }

        // 获取目标平台
        var targetPlatforms = new List<PlatformType>();
        
        if (all)
        {
            // 获取所有已配置账号的平台
            var sessions = await sessionService.GetAllAsync();
            targetPlatforms = sessions.Select(s => s.Platform).Distinct().ToList();
        }
        else if (!string.IsNullOrEmpty(platform))
        {
            var pt = ParsePlatform(platform);
            if (pt == null)
            {
                AnsiConsole.MarkupLine($"[red]错误: 无效的平台 - {platform}[/]");
                return;
            }
            targetPlatforms.Add(pt.Value);
        }
        else
        {
            AnsiConsole.MarkupLine("[red]错误: 请指定平台 (-p) 或使用 --all 发布到所有平台[/]");
            return;
        }

        if (!targetPlatforms.Any())
        {
            AnsiConsole.MarkupLine("[yellow]没有可发布的平台，请先添加账号[/]");
            return;
        }

        // 创建发布任务
        AnsiConsole.MarkupLine($"[blue]视频:[/] {video.Title}");
        AnsiConsole.MarkupLine($"[blue]目标平台:[/] {string.Join(", ", targetPlatforms.Select(p => p.GetDisplayName()))}");
        if (scheduledTime.HasValue)
        {
            AnsiConsole.MarkupLine($"[blue]定时发布:[/] {scheduledTime:yyyy-MM-dd HH:mm}");
        }

        var createdTaskIds = new List<long>();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在创建发布任务...", async ctx =>
            {
                foreach (var pt in targetPlatforms)
                {
                    try
                    {
                        var task = await publishService.CreateTaskAsync(videoId, pt, scheduledTime);
                        createdTaskIds.Add(task.Id);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]跳过 {pt.GetDisplayName()}: {ex.Message}[/]");
                    }
                }
            });

        if (createdTaskIds.Any())
        {
            AnsiConsole.MarkupLine($"[green]✓ 已创建 {createdTaskIds.Count} 个发布任务[/]");
            
            var table = new Table();
            table.AddColumn("任务ID");
            table.AddColumn("状态");

            foreach (var taskId in createdTaskIds)
            {
                table.AddRow(taskId.ToString(), "待发布");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("\n[dim]使用 'autopub scheduler start' 启动调度器开始发布[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]未创建任何任务[/]");
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
}
