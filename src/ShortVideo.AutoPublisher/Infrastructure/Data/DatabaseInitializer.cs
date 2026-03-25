using Dapper;
using Microsoft.Extensions.Logging;

namespace ShortVideo.AutoPublisher.Infrastructure.Data;

/// <summary>
/// 数据库初始化器
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext dbContext, ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("开始初始化数据库...");

            using var connection = _dbContext.CreateConnection();

            // 创建视频内容表
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS VideoContents (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Description TEXT,
                    Tags TEXT,
                    FilePath TEXT NOT NULL,
                    FileSize INTEGER NOT NULL DEFAULT 0,
                    Duration INTEGER NOT NULL DEFAULT 0,
                    CoverPath TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT
                )");

            // 创建Cookie会话表
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS CookieSessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Platform INTEGER NOT NULL,
                    AccountName TEXT NOT NULL,
                    CookieData TEXT NOT NULL,
                    IsDefault INTEGER NOT NULL DEFAULT 0,
                    IsValid INTEGER NOT NULL DEFAULT 1,
                    LastValidatedAt TEXT,
                    ExpiresAt TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT
                )");

            // 创建发布任务表
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS PublishTasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VideoId INTEGER NOT NULL,
                    VideoTitle TEXT NOT NULL,
                    Platform INTEGER NOT NULL,
                    SessionId INTEGER NOT NULL,
                    Status INTEGER NOT NULL DEFAULT 0,
                    Progress INTEGER NOT NULL DEFAULT 0,
                    ErrorMessage TEXT,
                    PublishedUrl TEXT,
                    ScheduledTime TEXT,
                    StartedAt TEXT,
                    CompletedAt TEXT,
                    CreatedAt TEXT NOT NULL,
                    RetryCount INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (VideoId) REFERENCES VideoContents(Id),
                    FOREIGN KEY (SessionId) REFERENCES CookieSessions(Id)
                )");

            // 创建封面图片表
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS CoverImages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    VideoId INTEGER NOT NULL,
                    Platform INTEGER NOT NULL,
                    FilePath TEXT NOT NULL,
                    Width INTEGER NOT NULL DEFAULT 0,
                    Height INTEGER NOT NULL DEFAULT 0,
                    FileSize INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (VideoId) REFERENCES VideoContents(Id)
                )");

            // 创建系统日志表
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS SystemLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Level TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    Exception TEXT,
                    CreatedAt TEXT NOT NULL
                )");

            // 创建索引
            await connection.ExecuteAsync(@"
                CREATE INDEX IF NOT EXISTS IX_PublishTasks_Status ON PublishTasks(Status);
                CREATE INDEX IF NOT EXISTS IX_PublishTasks_VideoId ON PublishTasks(VideoId);
                CREATE INDEX IF NOT EXISTS IX_CookieSessions_Platform ON CookieSessions(Platform);
                CREATE INDEX IF NOT EXISTS IX_CoverImages_VideoId ON CoverImages(VideoId);
            ");

            _logger.LogInformation("数据库初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }
}
