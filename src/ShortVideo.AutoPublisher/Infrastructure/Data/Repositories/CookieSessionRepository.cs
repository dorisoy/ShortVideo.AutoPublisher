using Dapper;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;

namespace ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;

/// <summary>
/// Cookie会话仓储
/// </summary>
public class CookieSessionRepository
{
    private readonly AppDbContext _dbContext;

    public CookieSessionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取所有会话
    /// </summary>
    public async Task<IEnumerable<CookieSession>> GetAllAsync()
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<CookieSession>(
            "SELECT * FROM CookieSessions ORDER BY Platform, CreatedAt DESC");
    }

    /// <summary>
    /// 根据ID获取会话
    /// </summary>
    public async Task<CookieSession?> GetByIdAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<CookieSession>(
            "SELECT * FROM CookieSessions WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 根据平台获取会话
    /// </summary>
    public async Task<IEnumerable<CookieSession>> GetByPlatformAsync(PlatformType platform)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryAsync<CookieSession>(
            "SELECT * FROM CookieSessions WHERE Platform = @Platform ORDER BY IsDefault DESC, CreatedAt DESC",
            new { Platform = (int)platform });
    }

    /// <summary>
    /// 获取平台默认会话
    /// </summary>
    public async Task<CookieSession?> GetDefaultByPlatformAsync(PlatformType platform)
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<CookieSession>(
            "SELECT * FROM CookieSessions WHERE Platform = @Platform AND IsDefault = 1 AND IsValid = 1",
            new { Platform = (int)platform });
    }

    /// <summary>
    /// 添加会话
    /// </summary>
    public async Task<long> AddAsync(CookieSession session)
    {
        using var connection = _dbContext.CreateConnection();
        session.CreatedAt = DateTime.Now;
        
        // 如果设为默认，先取消同平台其他默认
        if (session.IsDefault)
        {
            await connection.ExecuteAsync(
                "UPDATE CookieSessions SET IsDefault = 0 WHERE Platform = @Platform",
                new { Platform = (int)session.Platform });
        }
        
        var id = await connection.ExecuteScalarAsync<long>(
            @"INSERT INTO CookieSessions (Platform, AccountName, CookieData, IsDefault, IsValid, CreatedAt)
              VALUES (@Platform, @AccountName, @CookieData, @IsDefault, @IsValid, @CreatedAt);
              SELECT last_insert_rowid();",
            session);
        
        session.Id = id;
        return id;
    }

    /// <summary>
    /// 更新会话
    /// </summary>
    public async Task UpdateAsync(CookieSession session)
    {
        using var connection = _dbContext.CreateConnection();
        session.UpdatedAt = DateTime.Now;
        
        await connection.ExecuteAsync(
            @"UPDATE CookieSessions SET 
                AccountName = @AccountName,
                CookieData = @CookieData,
                IsValid = @IsValid,
                LastValidatedAt = @LastValidatedAt,
                ExpiresAt = @ExpiresAt,
                UpdatedAt = @UpdatedAt
              WHERE Id = @Id",
            session);
    }

    /// <summary>
    /// 设置为默认
    /// </summary>
    public async Task SetDefaultAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        
        // 获取当前会话的平台
        var session = await GetByIdAsync(id);
        if (session == null) return;
        
        // 取消同平台其他默认
        await connection.ExecuteAsync(
            "UPDATE CookieSessions SET IsDefault = 0 WHERE Platform = @Platform",
            new { Platform = (int)session.Platform });
        
        // 设置当前为默认
        await connection.ExecuteAsync(
            "UPDATE CookieSessions SET IsDefault = 1 WHERE Id = @Id",
            new { Id = id });
    }

    /// <summary>
    /// 更新验证状态
    /// </summary>
    public async Task UpdateValidationAsync(long id, bool isValid, DateTime? expiresAt = null)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync(
            @"UPDATE CookieSessions SET 
                IsValid = @IsValid,
                LastValidatedAt = @LastValidatedAt,
                ExpiresAt = @ExpiresAt
              WHERE Id = @Id",
            new { Id = id, IsValid = isValid, LastValidatedAt = DateTime.Now, ExpiresAt = expiresAt });
    }

    /// <summary>
    /// 删除会话
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        using var connection = _dbContext.CreateConnection();
        await connection.ExecuteAsync("DELETE FROM CookieSessions WHERE Id = @Id", new { Id = id });
    }

    /// <summary>
    /// 获取各平台会话数量
    /// </summary>
    public async Task<Dictionary<PlatformType, int>> GetPlatformCountsAsync()
    {
        using var connection = _dbContext.CreateConnection();
        var results = await connection.QueryAsync<(int Platform, int Count)>(
            "SELECT Platform, COUNT(*) as Count FROM CookieSessions WHERE IsValid = 1 GROUP BY Platform");
        
        return results.ToDictionary(r => (PlatformType)r.Platform, r => r.Count);
    }

    /// <summary>
    /// 获取过期会话数量
    /// </summary>
    public async Task<int> GetExpiredCountAsync()
    {
        using var connection = _dbContext.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM CookieSessions WHERE IsValid = 0 OR (ExpiresAt IS NOT NULL AND ExpiresAt < @Now)",
            new { Now = DateTime.Now });
    }
}
