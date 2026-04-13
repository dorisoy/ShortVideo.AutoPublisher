using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

namespace ShortVideo.AutoPublisher.Services;

/// <summary>
/// 视频内容服务
/// </summary>
public class VideoContentService
{
    private readonly VideoContentRepository _repository;
    private readonly ILogger<VideoContentService> _logger;

    public VideoContentService(VideoContentRepository repository, ILogger<VideoContentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<VideoContent>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<VideoContent?> GetByIdAsync(long id) => await _repository.GetByIdAsync(id);

    public async Task<VideoContent> AddAsync(string filePath, string title, string? description = null, string? tags = null)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("视频文件不存在", filePath);

        var video = new VideoContent
        {
            Title = title,
            Description = description,
            Tags = tags,
            FilePath = filePath,
            FileSize = fileInfo.Length
        };

        await _repository.AddAsync(video);
        _logger.LogInformation("视频已添加: {Title}, ID: {Id}", title, video.Id);
        return video;
    }

    public async Task DeleteAsync(long id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("视频已删除: {Id}", id);
    }

    /// <summary>
    /// 搜索视频
    /// </summary>
    public async Task<IEnumerable<VideoContent>> SearchAsync(string keyword)
    {
        var all = await _repository.GetAllAsync();
        var lower = keyword.ToLower();
        return all.Where(v =>
            (v.Title?.ToLower().Contains(lower) == true) ||
            (v.Description?.ToLower().Contains(lower) == true) ||
            (v.Tags?.ToLower().Contains(lower) == true));
    }

    /// <summary>
    /// 更新视频信息
    /// </summary>
    public async Task UpdateAsync(VideoContent video)
    {
        await _repository.UpdateAsync(video);
        _logger.LogInformation("视频已更新: {Id}", video.Id);
    }
}

/// <summary>
/// Cookie 会话服务
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

    public async Task<IEnumerable<CookieSession>> GetAllAsync() => await _repository.GetAllAsync();

    public async Task<CookieSession> AddAsync(PlatformType platform, string accountName, string cookieData, bool isDefault = false)
    {
        var session = new CookieSession
        {
            Platform = platform,
            AccountName = accountName,
            CookieData = cookieData,
            IsDefault = isDefault,
            IsValid = true
        };

        await _repository.AddAsync(session);
        _logger.LogInformation("账号已添加: {Platform} - {Name}, ID: {Id}", platform, accountName, session.Id);
        return session;
    }

    public async Task DeleteAsync(long id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("账号已删除: {Id}", id);
    }

    public async Task RefreshStatusAsync(long id)
    {
        var session = await _repository.GetByIdAsync(id);
        if (session != null)
        {
            session.LastValidatedAt = DateTime.Now;
            session.IsValid = !string.IsNullOrEmpty(session.CookieData);
            await _repository.UpdateAsync(session);
            _logger.LogInformation("账号状态已刷新: {Id}", id);
        }
    }
}

