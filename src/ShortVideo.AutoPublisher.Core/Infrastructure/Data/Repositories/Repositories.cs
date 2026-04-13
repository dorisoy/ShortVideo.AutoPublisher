using Dapper;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Infrastructure.Data;

namespace ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

/// <summary>
/// 视频内容仓储
/// </summary>
public class VideoContentRepository
{
    private readonly AppDbContext _context;

    public VideoContentRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<VideoContent>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<VideoContent>("SELECT * FROM VideoContents ORDER BY CreatedAt DESC");
    }

    public async Task<VideoContent?> GetByIdAsync(long id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<VideoContent>("SELECT * FROM VideoContents WHERE Id = @Id", new { Id = id });
    }

    public async Task<VideoContent> AddAsync(VideoContent video)
    {
        using var conn = _context.CreateConnection();
        video.Id = await conn.QuerySingleAsync<long>(
            "INSERT INTO VideoContents (Title, Description, Tags, FilePath, FileSize) VALUES (@Title, @Description, @Tags, @FilePath, @FileSize); SELECT last_insert_rowid()",
            video);
        return video;
    }

    public async Task DeleteAsync(long id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM VideoContents WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 更新视频信息
    /// </summary>
    public async Task UpdateAsync(VideoContent video)
    {
        using var conn = _context.CreateConnection();
        video.UpdatedAt = DateTime.Now;
        await conn.ExecuteAsync(
            "UPDATE VideoContents SET Title = @Title, Description = @Description, Tags = @Tags, UpdatedAt = @UpdatedAt WHERE Id = @Id",
            video);
    }
}

/// <summary>
/// Cookie 会话仓储
/// </summary>
public class CookieSessionRepository
{
    private readonly AppDbContext _context;

    public CookieSessionRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<CookieSession>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<CookieSession>("SELECT * FROM CookieSessions ORDER BY CreatedAt DESC");
    }

    public async Task<CookieSession?> GetByIdAsync(long id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CookieSession>("SELECT * FROM CookieSessions WHERE Id = @Id", new { Id = id });
    }

    public async Task<CookieSession?> GetDefaultByPlatformAsync(PlatformType platform)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<CookieSession>(
            "SELECT * FROM CookieSessions WHERE Platform = @Platform AND IsDefault = 1 LIMIT 1",
            new { Platform = (int)platform });
    }

    public async Task<CookieSession> AddAsync(CookieSession session)
    {
        using var conn = _context.CreateConnection();
        session.Id = await conn.QuerySingleAsync<long>(
            "INSERT INTO CookieSessions (Platform, AccountName, CookieData, IsValid, IsDefault) VALUES (@Platform, @AccountName, @CookieData, @IsValid, @IsDefault); SELECT last_insert_rowid()",
            new { Platform = (int)session.Platform, session.AccountName, session.CookieData, IsValid = session.IsValid ? 1 : 0, IsDefault = session.IsDefault ? 1 : 0 });
        return session;
    }

    public async Task UpdateAsync(CookieSession session)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE CookieSessions SET AccountName = @AccountName, CookieData = @CookieData, IsValid = @IsValid, IsDefault = @IsDefault, UpdatedAt = datetime('now','localtime') WHERE Id = @Id",
            new { session.AccountName, session.CookieData, IsValid = session.IsValid ? 1 : 0, IsDefault = session.IsDefault ? 1 : 0, session.Id });
    }

    public async Task DeleteAsync(long id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM CookieSessions WHERE Id = @Id", new { Id = id });
    }
}

/// <summary>
/// 发布任务仓储
/// </summary>
public class PublishTaskRepository
{
    private readonly AppDbContext _context;

    public PublishTaskRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<PublishTask>> GetAllAsync()
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<PublishTask>("SELECT * FROM PublishTasks ORDER BY CreatedAt DESC");
    }

    public async Task<PublishTask?> GetByIdAsync(long id)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<PublishTask>("SELECT * FROM PublishTasks WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<PublishTask>> GetByStatusAsync(PublishTaskStatus status)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<PublishTask>("SELECT * FROM PublishTasks WHERE Status = @Status ORDER BY CreatedAt DESC",
            new { Status = (int)status });
    }

    public async Task<IEnumerable<PublishTask>> GetRecentTasksAsync(int count)
    {
        using var conn = _context.CreateConnection();
        return await conn.QueryAsync<PublishTask>("SELECT * FROM PublishTasks ORDER BY CreatedAt DESC LIMIT @Count", new { Count = count });
    }

    public async Task<PublishTask> AddAsync(PublishTask task)
    {
        using var conn = _context.CreateConnection();
        task.Id = await conn.QuerySingleAsync<long>(
            "INSERT INTO PublishTasks (VideoId, VideoTitle, Platform, SessionId, Status, ScheduledTime) VALUES (@VideoId, @VideoTitle, @Platform, @SessionId, @Status, @ScheduledTime); SELECT last_insert_rowid()",
            new { task.VideoId, task.VideoTitle, Platform = (int)task.Platform, task.SessionId, Status = (int)task.Status, task.ScheduledTime });
        return task;
    }

    public async Task UpdateAsync(PublishTask task)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE PublishTasks SET Status = @Status, Progress = @Progress, RetryCount = @RetryCount, ErrorMessage = @ErrorMessage, PublishedUrl = @PublishedUrl, StartedAt = @StartedAt, CompletedAt = @CompletedAt WHERE Id = @Id",
            new { Status = (int)task.Status, task.Progress, task.RetryCount, task.ErrorMessage, task.PublishedUrl, task.StartedAt, task.CompletedAt, task.Id });
    }

    public async Task DeleteAsync(long id)
    {
        using var conn = _context.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM PublishTasks WHERE Id = @Id", new { Id = id });
    }
}
