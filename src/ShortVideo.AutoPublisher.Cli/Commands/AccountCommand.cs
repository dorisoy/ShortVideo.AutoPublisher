using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;
using Spectre.Console;

namespace ShortVideo.AutoPublisher.Cli.Commands;

/// <summary>
/// 账号管理命令
/// </summary>
public static class AccountCommand
{
    public static Command Create(IServiceProvider services)
    {
        var command = new Command("account", "平台账号管理");

        // list 子命令
        var listCommand = new Command("list", "列出所有账号");
        listCommand.SetHandler(async () => await ListAccountsAsync(services));
        command.AddCommand(listCommand);

        // add 子命令
        var addCommand = new Command("add", "添加账号");
        var platformOption = new Option<string>(new[] { "-p", "--platform" }, "平台 (douyin/xiaohongshu/baijiahao/weixin/toutiao)") { IsRequired = true };
        var nameOption = new Option<string>(new[] { "-n", "--name" }, "账号名称") { IsRequired = true };
        var cookieOption = new Option<string>(new[] { "-c", "--cookie" }, "Cookie数据") { IsRequired = true };
        var defaultOption = new Option<bool>(new[] { "-d", "--default" }, () => true, "设为默认账号");
        addCommand.AddOption(platformOption);
        addCommand.AddOption(nameOption);
        addCommand.AddOption(cookieOption);
        addCommand.AddOption(defaultOption);
        addCommand.SetHandler(async (platform, name, cookie, isDefault) =>
            await AddAccountAsync(services, platform, name, cookie, isDefault),
            platformOption, nameOption, cookieOption, defaultOption);
        command.AddCommand(addCommand);

        // delete 子命令
        var deleteCommand = new Command("delete", "删除账号");
        var idArg = new Argument<long>("id", "账号ID");
        deleteCommand.AddArgument(idArg);
        deleteCommand.SetHandler(async (id) => await DeleteAccountAsync(services, id), idArg);
        command.AddCommand(deleteCommand);

        // validate 子命令
        var validateCommand = new Command("validate", "验证账号");
        var validateIdArg = new Argument<long>("id", "账号ID");
        validateCommand.AddArgument(validateIdArg);
        validateCommand.SetHandler(async (id) => await ValidateAccountAsync(services, id), validateIdArg);
        command.AddCommand(validateCommand);

        return command;
    }

    private static async Task ListAccountsAsync(IServiceProvider services)
    {
        var sessionService = services.GetRequiredService<CookieSessionService>();
        var sessions = await sessionService.GetAllAsync();

        if (!sessions.Any())
        {
            AnsiConsole.MarkupLine("[yellow]暂无账号[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("平台");
        table.AddColumn("账号名称");
        table.AddColumn("状态");
        table.AddColumn("默认");
        table.AddColumn("更新时间");

        foreach (var session in sessions)
        {
            var statusColor = session.IsValid ? "green" : "red";
            var statusText = session.IsValid ? "有效" : "无效";
            var defaultMark = session.IsDefault ? "[green]✓[/]" : "-";
            
            table.AddRow(
                session.Id.ToString(),
                session.Platform.GetDisplayName(),
                session.AccountName ?? "-",
                $"[{statusColor}]{statusText}[/]",
                defaultMark,
                session.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\n[green]共 {sessions.Count()} 个账号[/]");
    }

    private static async Task AddAccountAsync(IServiceProvider services, string platform, string name, string cookie, bool isDefault)
    {
        var platformType = ParsePlatform(platform);
        if (platformType == null)
        {
            AnsiConsole.MarkupLine($"[red]错误: 无效的平台 - {platform}[/]");
            AnsiConsole.MarkupLine("[yellow]支持的平台: douyin, xiaohongshu, baijiahao, weixin, toutiao[/]");
            return;
        }

        try
        {
            var sessionService = services.GetRequiredService<CookieSessionService>();
            var session = await sessionService.AddAsync(platformType.Value, name, cookie, isDefault);
            AnsiConsole.MarkupLine($"[green]✓ 账号添加成功, ID: {session.Id}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
        }
    }

    private static async Task DeleteAccountAsync(IServiceProvider services, long id)
    {
        var sessionService = services.GetRequiredService<CookieSessionService>();
        var sessions = await sessionService.GetAllAsync();
        var session = sessions.FirstOrDefault(s => s.Id == id);
        
        if (session == null)
        {
            AnsiConsole.MarkupLine($"[red]错误: 账号不存在 (ID: {id})[/]");
            return;
        }

        if (!AnsiConsole.Confirm($"确定要删除账号 [{session.AccountName}] 吗?"))
        {
            return;
        }

        await sessionService.DeleteAsync(id);
        AnsiConsole.MarkupLine($"[green]✓ 账号已删除[/]");
    }

    private static async Task ValidateAccountAsync(IServiceProvider services, long id)
    {
        var sessionService = services.GetRequiredService<CookieSessionService>();
        var sessions = await sessionService.GetAllAsync();
        var session = sessions.FirstOrDefault(s => s.Id == id);
        
        if (session == null)
        {
            AnsiConsole.MarkupLine($"[red]错误: 账号不存在 (ID: {id})[/]");
            return;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("正在验证账号...", async ctx =>
            {
                await sessionService.RefreshStatusAsync(id);
            });

        AnsiConsole.MarkupLine($"[green]✓ 账号 [{session.AccountName}] 验证通过[/]");
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
