using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Browser;

namespace ShortVideo.AutoPublisher.OpenClaw.Agents;

/// <summary>
/// 代理工厂
/// </summary>
public class AgentFactory : IAgentFactory
{
    private readonly BrowserManager _browserManager;
    private readonly AppSettings _settings;
    private readonly IServiceProvider _serviceProvider;

    public AgentFactory(BrowserManager browserManager, AppSettings settings, IServiceProvider serviceProvider)
    {
        _browserManager = browserManager;
        _settings = settings;
        _serviceProvider = serviceProvider;
    }

    public IAiAgent Create(PlatformType platform)
    {
        return platform switch
        {
            PlatformType.Douyin => new DouyinAgent(_browserManager, _settings, _serviceProvider.GetRequiredService<ILogger<DouyinAgent>>()),
            PlatformType.Xiaohongshu => new XiaohongshuAgent(_browserManager, _settings, _serviceProvider.GetRequiredService<ILogger<XiaohongshuAgent>>()),
            _ => throw new NotSupportedException($"暂不支持平台: {platform.GetDisplayName()}")
        };
    }
}
