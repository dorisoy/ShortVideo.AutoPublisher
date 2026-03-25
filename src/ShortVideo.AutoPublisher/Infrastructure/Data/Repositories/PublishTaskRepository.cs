using Dapper;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

/// <summary>
/// 发布任务仓储
/// </summary>
public class PublishTaskRepository
{
    private readonly AppDbContext _dbContext;

    public PublishTaskRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取所有任务
    /// </summary>
    public async Task<IEnumerable<PublishTask>> GetAllAsync()
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<PublishTask>(
            "SELECT * FROM PublishTasks ORDER BY CreatedAt DESC");
    }

    /// <summary>
    /// 根据ID获取任务
    /// </summary>
    public async Task<PublishTask?> GetByIdAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PublishTask>(
            "SELECT * FROM PublishTasks WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 根据状态获取任务
    /// </summary>
    public async Task<IEnumerable<PublishTask>> GetByStatusAsync(PublishTaskStatus status)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<PublishTask>(
            "SELECT * FROM PublishTasks WHERE Status = @Status ORDER BY CreatedAt DESC",
            new { Status = (int)status });
    }

    /// <summary>
    /// 获取待执行任务
    /// </summary>
    public async Task<IEnumerable<PublishTask>> GetPendingTasksAsync()
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<PublishTask>(
            @"SELECT * FROM PublishTasks 
              WHERE Status = @Status 
                AND (ScheduledTime IS NULL OR ScheduledTime <= @Now)
              ORDER BY CreatedAt ASC",
            new { Status = (int)PublishTaskStatus.Pending, Now = DateTime.Now });
    }

    /// <summary>
    /// 获取最近任务
    /// </summary>
    public async Task<IEnumerable<PublishTask>> GetRecentTasksAsync(int count = 5)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<PublishTask>(
            "SELECT * FROM PublishTasks ORDER BY CreatedAt DESC LIMIT @Count",
            new { Count = count });
    }

    /// <summary>
    /// 添加任务
    /// </summary>
    public async Task<long> AddAsync(PublishTask task)
    {
        using var connection = _dbContext.CreateConnection();
        task.CreatedAt = DateTime.Now;
        
        var id = await connection.ExecuteScalarAsync<long>(
            @"INSERT INTO PublishTasks (VideoId, VideoTitle, Platform, SessionId, Status, Progress, ScheduledTime, CreatedAt)
              VALUES (@VideoId, @VideoTitle, @Platform, @SessionId, @Status, @Progress, @ScheduledTime, @CreatedAt);
              SELECT last_insert_rowid();",
            task);
        
        task.Id = id;
        return id;
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    public async Task UpdateStatusAsync(long id, PublishTaskStatus status, int progress = 0, string? errorMessage = null)
    {
        using var connection = _dbContext.CreateConnection();
        
        var completedAt = status.IsTerminal() ? DateTime.Now : (DateTime?)null;
        var startedAt = status == PublishTaskStatus.Running ? DateTime.Now : (DateTime?)null;
        
        await connection.ExecuteAsync(
            @"UPDATE PublishTasks SET 
                Status = @Status,
                Progress = @Progress,
                ErrorMessage = @ErrorMessage,
                StartedAt = COALESCE(@StartedAt, StartedAt),
                CompletedAt = @CompletedAt
              WHERE Id = @Id",
            new { Id = id, Status = (int)status, Progress = progress, ErrorMessage = errorMessage, StartedAt = startedAt, CompletedAt = completedAt });
    }

    /// <summary>
    /// 更新任务进度
    /// </summary>
    public async Task UpdateProgressAsync(long id, int progress)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE PublishTasks SET Progress = @Progress WHERE Id = @Id",
            new { Id = id, Progress = progress });
    }

    /// <summary>
    /// 设置发布URL
    /// </summary>
    public async Task SetPublishedUrlAsync(long id, string url)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE PublishTasks SET PublishedUrl = @Url WHERE Id = @Id",
            new { Id = id, Url = url });
    }

    /// <summary>
    /// 增加重试次数
    /// </summary>
    public async Task IncrementRetryCountAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(
            "UPDATE PublishTasks SET RetryCount = RetryCount + 1, Status = @Status WHERE Id = @Id",
            new { Id = id, Status = (int)PublishTaskStatus.Pending });
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM PublishTasks WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 获取各状态任务数量
    /// </summary>
    public async Task<Dictionary<PublishTaskStatus, int>> GetStatusCountsAsync()
    {
        using var connection = _dbContext.CreateConnection();
        var results = await connection.QueryAsync<(int Status, int Count)>(
            "SELECT Status, COUNT(*) as Count FROM PublishTasks GROUP BY Status");
        
        return results.ToDictionary(r => (PublishTaskStatus)r.Status, r => r.Count);
    }

    /// <summary>
    /// 获取今日发布数
    /// </summary>
    public async Task<int> GetTodayCompletedCountAsync()
    {
        using var connection = _dbContext.CreateConnection();
        var today = DateTime.Today;
        return await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM PublishTasks 
              WHERE Status = @Status AND date(CompletedAt) = date(@Today)",
            new { Status = (int)PublishTaskStatus.Completed, Today = today });
    }
}
