using Dapper;
using ShortVideo.AutoPublisher.Domain.Entities;

namespace ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

/// <summary>
/// 视频内容仓储
/// </summary>
public class VideoContentRepository
{
    private readonly AppDbContext _dbContext;

    public VideoContentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取所有视频
    /// </summary>
    public async Task<IEnumerable<VideoContent>> GetAllAsync()
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<VideoContent>(
            "SELECT * FROM VideoContents ORDER BY CreatedAt DESC");
    }

    /// <summary>
    /// 根据ID获取视频
    /// </summary>
    public async Task<VideoContent?> GetByIdAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<VideoContent>(
            "SELECT * FROM VideoContents WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 搜索视频
    /// </summary>
    public async Task<IEnumerable<VideoContent>> SearchAsync(string keyword)
    {
        using var connection = _dbContext.CreateConnection();
        var searchPattern = $"%{keyword}%";
        return await connection.QueryAsync<VideoContent>(
            @"SELECT * FROM VideoContents 
              WHERE Title LIKE @Pattern OR Tags LIKE @Pattern 
              ORDER BY CreatedAt DESC",
            new { Pattern = searchPattern });
    }

    /// <summary>
    /// 添加视频
    /// </summary>
    public async Task<long> AddAsync(VideoContent video)
    {
        using var connection = _dbContext.CreateConnection();
        video.CreatedAt = DateTime.Now;
        
        var id = await connection.ExecuteScalarAsync<long>(
            @"INSERT INTO VideoContents (Title, Description, Tags, FilePath, FileSize, Duration, CoverPath, CreatedAt)
              VALUES (@Title, @Description, @Tags, @FilePath, @FileSize, @Duration, @CoverPath, @CreatedAt);
              SELECT last_insert_rowid();",
            video);
        
        video.Id = id;
        return id;
    }

    /// <summary>
    /// 更新视频
    /// </summary>
    public async Task UpdateAsync(VideoContent video)
    {
        using var connection = _dbContext.CreateConnection();
        video.UpdatedAt = DateTime.Now;
        
        await connection.ExecuteAsync(
            @"UPDATE VideoContents SET 
                Title = @Title,
                Description = @Description,
                Tags = @Tags,
                FilePath = @FilePath,
                FileSize = @FileSize,
                Duration = @Duration,
                CoverPath = @CoverPath,
                UpdatedAt = @UpdatedAt
              WHERE Id = @Id",
            video);
    }

    /// <summary>
    /// 删除视频
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM VideoContents WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 获取视频总数
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM VideoContents");
    }
}
