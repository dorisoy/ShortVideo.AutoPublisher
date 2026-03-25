using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Infrastructure.Data;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;
using ShortVideo.AutoPublisher.Mcp.Tools;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Agents;
using ShortVideo.AutoPublisher.OpenClaw.Browser;
using ShortVideo.AutoPublisher.OpenClaw.TaskScheduler;
using ShortVideo.AutoPublisher.Services;

namespace ShortVideo.AutoPublisher.Mcp;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // 配置日志 - MCP 服务器需要静默
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // 配置
        builder.Services.AddSingleton(new AppSettings());

        // 数据库
        builder.Services.AddSingleton<AppDbContext>();
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddSingleton<VideoContentRepository>();
        builder.Services.AddSingleton<CookieSessionRepository>();
        builder.Services.AddSingleton<PublishTaskRepository>();

        // 服务
        builder.Services.AddSingleton<VideoContentService>();
        builder.Services.AddSingleton<CookieSessionService>();
        builder.Services.AddSingleton<PublishService>();
        
        // OpenClaw 代理
        builder.Services.AddSingleton<BrowserManager>();
        builder.Services.AddSingleton<IAgentFactory, AgentFactory>();
        builder.Services.AddSingleton<PublishTaskScheduler>();

        // 配置 MCP Server
        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        var host = builder.Build();

        // 初始化数据库
        var dbInitializer = host.Services.GetRequiredService<DatabaseInitializer>();
        await dbInitializer.InitializeAsync();

        await host.RunAsync();
    }
}
