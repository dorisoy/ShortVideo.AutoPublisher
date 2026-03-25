using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Browser;

namespace ShortVideo.AutoPublisher.OpenClaw.Agents;

/// <summary>
/// AI代理工厂
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

    /// <summary>
    /// 创建指定平台的代理
    /// </summary>
    public IAiAgent CreateAgent(PlatformType platform)
    {
        return platform switch
        {
            PlatformType.Douyin => new DouyinAgent(
                _browserManager, 
                _settings, 
                _serviceProvider.GetRequiredService<ILogger<DouyinAgent>>()),
            
            PlatformType.Xiaohongshu => new XiaohongshuAgent(
                _browserManager, 
                _settings, 
                _serviceProvider.GetRequiredService<ILogger<XiaohongshuAgent>>()),
            
            PlatformType.Baijiahao => new BaijiahaoAgent(
                _browserManager, 
                _settings, 
                _serviceProvider.GetRequiredService<ILogger<BaijiahaoAgent>>()),
            
            PlatformType.WeixinChannel => new WeixinChannelAgent(
                _browserManager, 
                _settings, 
                _serviceProvider.GetRequiredService<ILogger<WeixinChannelAgent>>()),
            
            PlatformType.Toutiao => new ToutiaoAgent(
                _browserManager, 
                _settings, 
                _serviceProvider.GetRequiredService<ILogger<ToutiaoAgent>>()),
            
            _ => throw new NotSupportedException($"不支持的平台: {platform}")
        };
    }

    /// <summary>
    /// 获取支持的平台列表
    /// </summary>
    public IEnumerable<PlatformType> GetSupportedPlatforms()
    {
        return
        [
            PlatformType.Douyin,
            PlatformType.Xiaohongshu,
            PlatformType.Baijiahao,
            PlatformType.WeixinChannel,
            PlatformType.Toutiao
        ];
    }
}
