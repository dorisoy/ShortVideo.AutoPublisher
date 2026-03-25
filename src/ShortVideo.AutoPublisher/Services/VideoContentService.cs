using System.IO;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Domain.Entities;
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

    /// <summary>
    /// 获取所有视频
    /// </summary>
    public async Task<IEnumerable<VideoContent>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    /// <summary>
    /// 根据ID获取视频
    /// </summary>
    public async Task<VideoContent?> GetByIdAsync(long id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// 搜索视频
    /// </summary>
    public async Task<IEnumerable<VideoContent>> SearchAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return await _repository.GetAllAsync();
        }
        return await _repository.SearchAsync(keyword);
    }

    /// <summary>
    /// 添加视频
    /// </summary>
    public async Task<VideoContent> AddAsync(string filePath, string title, string? description = null, string? tags = null)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("视频文件不存在", filePath);
        }

        var video = new VideoContent
        {
            Title = title,
            Description = description,
            Tags = tags,
            FilePath = filePath,
            FileSize = fileInfo.Length
        };

        await _repository.AddAsync(video);
        _logger.LogInformation("视频已添加: {Title}", title);
        return video;
    }

    /// <summary>
    /// 更新视频
    /// </summary>
    public async Task UpdateAsync(VideoContent video)
    {
        await _repository.UpdateAsync(video);
        _logger.LogInformation("视频已更新: {Id}", video.Id);
    }

    /// <summary>
    /// 删除视频
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("视频已删除: {Id}", id);
    }

    /// <summary>
    /// 获取视频总数
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        return await _repository.GetCountAsync();
    }
}

