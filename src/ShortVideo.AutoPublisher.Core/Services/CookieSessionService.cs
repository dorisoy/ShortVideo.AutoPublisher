using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

namespace ShortVideo.AutoPublisher.Services;

/// <summary>
/// Cookie会话服务
/// </summary>
public class CookieSessionService
{
    private readonly CookieSessionRepository _repository;
    private readonly ILogger<CookieSessionService> _logger;

    public CookieSessionService(CookieSessionRepository repository, ILogger<CookieSessionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有会话
    /// </summary>
    public async Task<IEnumerable<CookieSession>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    /// <summary>
    /// 根据平台获取会话
    /// </summary>
    public async Task<IEnumerable<CookieSession>> GetByPlatformAsync(PlatformType platform)
    {
        return await _repository.GetByPlatformAsync(platform);
    }

    /// <summary>
    /// 获取平台默认会话
    /// </summary>
    public async Task<CookieSession?> GetDefaultByPlatformAsync(PlatformType platform)
    {
        return await _repository.GetDefaultByPlatformAsync(platform);
    }

    /// <summary>
    /// 添加会话
    /// </summary>
    public async Task<CookieSession> AddAsync(PlatformType platform, string accountName, string cookieData, bool isDefault = true)
    {
        var session = new CookieSession
        {
            Platform = platform,
            AccountName = accountName,
            CookieData = cookieData,
            IsDefault = isDefault
        };

        await _repository.AddAsync(session);
        _logger.LogInformation("Cookie会话已添加: {Platform} - {Account}", platform.GetDisplayName(), accountName);
        return session;
    }

    /// <summary>
    /// 更新会话
    /// </summary>
    public async Task UpdateAsync(CookieSession session)
    {
        await _repository.UpdateAsync(session);
        _logger.LogInformation("Cookie会话已更新: {Id}", session.Id);
    }

    /// <summary>
    /// 设置为默认
    /// </summary>
    public async Task SetDefaultAsync(long id)
    {
        await _repository.SetDefaultAsync(id);
        _logger.LogInformation("Cookie会话已设为默认: {Id}", id);
    }

    /// <summary>
    /// 刷新会话状态
    /// </summary>
    public async Task RefreshStatusAsync(long id)
    {
        // 这里可以添加实际的Cookie验证逻辑
        await _repository.UpdateValidationAsync(id, true);
        _logger.LogInformation("Cookie会话状态已刷新: {Id}", id);
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Cookie会话已删除: {Id}", id);
    }

    /// <summary>
    /// 获取各平台会话数量
    /// </summary>
    public async Task<Dictionary<PlatformType, int>> GetPlatformCountsAsync()
    {
        return await _repository.GetPlatformCountsAsync();
    }

    /// <summary>
    /// 获取过期会话数量
    /// </summary>
    public async Task<int> GetExpiredCountAsync()
    {
        return await _repository.GetExpiredCountAsync();
    }
}

