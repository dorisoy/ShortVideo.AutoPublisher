using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Cli.Commands;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Infrastructure.Data;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;
using ShortVideo.AutoPublisher.Services;
using ShortVideo.AutoPublisher.OpenClaw.TaskScheduler;
using ShortVideo.AutoPublisher.OpenClaw.Browser;
using ShortVideo.AutoPublisher.OpenClaw.Agents;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;

namespace ShortVideo.AutoPublisher.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 配置依赖注入
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();

        // 初始化数据库
        var dbInitializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        await dbInitializer.InitializeAsync();

        // 创建根命令
        var rootCommand = new RootCommand("ShortVideo AutoPublisher CLI - 短视频自动发布命令行工具");

        // 添加子命令
        rootCommand.Add(VideoCommand.Create(serviceProvider));
        rootCommand.Add(AccountCommand.Create(serviceProvider));
        rootCommand.Add(PublishCommand.Create(serviceProvider));
        rootCommand.Add(TaskCommand.Create(serviceProvider));
        rootCommand.Add(SchedulerCommand.Create(serviceProvider));

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync(parseResult.InvocationConfiguration);
    }

    private static ServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // 日志
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // 配置
        services.AddSingleton(new AppSettings());

        // 数据库
        services.AddSingleton<AppDbContext>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<VideoContentRepository>();
        services.AddSingleton<CookieSessionRepository>();
        services.AddSingleton<PublishTaskRepository>();

        // 服务
        services.AddSingleton<VideoContentService>();
        services.AddSingleton<CookieSessionService>();
        services.AddSingleton<PublishService>();
        
        // OpenClaw 代理
        services.AddSingleton<BrowserManager>();
        services.AddSingleton<IAgentFactory, AgentFactory>();
        services.AddSingleton<PublishTaskScheduler>();

        return services;
    }
}
