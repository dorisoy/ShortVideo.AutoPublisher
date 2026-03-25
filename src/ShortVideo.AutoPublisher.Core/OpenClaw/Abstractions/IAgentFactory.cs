using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.OpenClaw.Abstractions;

/// <summary>
/// AI代理工厂接口
/// </summary>
public interface IAgentFactory
{
    /// <summary>
    /// 创建指定平台的代理
    /// </summary>
    IAiAgent CreateAgent(PlatformType platform);

    /// <summary>
    /// 获取支持的平台列表
    /// </summary>
    IEnumerable<PlatformType> GetSupportedPlatforms();
}
