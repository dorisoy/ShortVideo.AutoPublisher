using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ShortVideo.AutoPublisher.Services;
using Spectre.Console;

namespace ShortVideo.AutoPublisher.Cli.Commands;

/// <summary>
/// 视频管理命令
/// </summary>
public static class VideoCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("video", "视频库管理");

        // list 子命令
        var listCommand = new Command("list", "列出所有视频");
        listCommand.SetHandler(async () => await ListVideosAsync(services));
        command.AddCommand(listCommand);

        // add 子命令
        var addCommand = new Command("add", "添加视频");
        var fileOption = new Option<string>(new[] { "-f", "--file" }, "视频文件路径") { IsRequired = true };
        var titleOption = new Option<string>(new[] { "-t", "--title" }, "视频标题") { IsRequired = true };
        var descOption = new Option<string>(new[] { "-d", "--description" }, () => string.Empty, "视频描述");
        var tagsOption = new Option<string>(new[] { "--tags" }, () => string.Empty, "标签 (逗号分隔)");
        addCommand.AddOption(fileOption);
        addCommand.AddOption(titleOption);
        addCommand.AddOption(descOption);
        addCommand.AddOption(tagsOption);
        addCommand.SetHandler(async (file, title, desc, tags) => 
            await AddVideoAsync(services, file, title, desc, tags),
            fileOption, titleOption, descOption, tagsOption);
        command.AddCommand(addCommand);

        // delete 子命令
        var deleteCommand = new Command("delete", "删除视频");
        var idArg = new Argument<long>("id", "视频ID");
        deleteCommand.AddArgument(idArg);
        deleteCommand.SetHandler(async (id) => await DeleteVideoAsync(services, id), idArg);
        command.AddCommand(deleteCommand);

        // info 子命令
        var infoCommand = new Command("info", "查看视频详情");
        var infoIdArg = new Argument<long>("id", "视频ID");
        infoCommand.AddArgument(infoIdArg);
        infoCommand.SetHandler(async (id) => await ShowVideoInfoAsync(services, id), infoIdArg);
        command.AddCommand(infoCommand);

        return command;
    }

    private static async Task ListVideosAsync(IServiceProvider services)
    {
        var videoService = services.GetRequiredService<VideoContentService>();
        var videos = await videoService.GetAllAsync();

        if (!videos.Any())
        {
            AnsiConsole.MarkupLine("[yellow]暂无视频[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("标题");
        table.AddColumn("标签");
        table.AddColumn("文件大小");
        table.AddColumn("创建时间");

        foreach (var video in videos)
        {
            table.AddRow(
                video.Id.ToString(),
                video.Title.Length > 30 ? video.Title[..27] + "..." : video.Title,
                video.Tags ?? "-",
                video.FormattedFileSize,
                video.CreatedAt.ToString("yyyy-MM-dd HH:mm")
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[green]共 {videos.Count()} 个视频[/]");
    }

    private static async Task AddVideoAsync(IServiceProvider services, string filePath, string title, string description, string tags)
    {
        if (!File.Exists(filePath))
        {
            AnsiConsole.MarkupLine($"[red]错误: 文件不存在 - {filePath}[/]");
            return;
        }

        try
        {
            var videoService = services.GetRequiredService<VideoContentService>();
            var video = await videoService.AddAsync(
                Path.GetFullPath(filePath),
                title,
                string.IsNullOrWhiteSpace(description) ? null : description,
                string.IsNullOrWhiteSpace(tags) ? null : tags
            );
            AnsiConsole.MarkupLine($"[green]✓ 视频添加成功, ID: {video.Id}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
        }
    }

    private static async Task DeleteVideoAsync(IServiceProvider services, long id)
    {
        var videoService = services.GetRequiredService<VideoContentService>();
        var video = await videoService.GetByIdAsync(id);
        
        if (video == null)
        {
            AnsiConsole.MarkupLine($"[red]错误: 视频不存在 (ID: {id})[/]");
            return;
        }

        if (!AnsiConsole.Confirm($"确定要删除视频 [{video.Title}] 吗?"))
        {
            return;
        }

        await videoService.DeleteAsync(id);
        AnsiConsole.MarkupLine($"[green]✓ 视频已删除[/]");
    }

    private static async Task ShowVideoInfoAsync(IServiceProvider services, long id)
    {
        var videoService = services.GetRequiredService<VideoContentService>();
        var video = await videoService.GetByIdAsync(id);
        
        if (video == null)
        {
            AnsiConsole.MarkupLine($"[red]错误: 视频不存在 (ID: {id})[/]");
            return;
        }

        var panel = new Panel(new Markup(
            $"[bold]ID:[/] {video.Id}\n" +
            $"[bold]标题:[/] {video.Title}\n" +
            $"[bold]描述:[/] {video.Description ?? "-"}\n" +
            $"[bold]标签:[/] {video.Tags ?? "-"}\n" +
            $"[bold]文件路径:[/] {video.FilePath}\n" +
            $"[bold]文件大小:[/] {video.FormattedFileSize}\n" +
            $"[bold]创建时间:[/] {video.CreatedAt:yyyy-MM-dd HH:mm:ss}"
        ))
        {
            Header = new PanelHeader("视频详情"),
            Border = BoxBorder.Rounded
        };

        AnsiConsole.Write(panel);
    }
}
