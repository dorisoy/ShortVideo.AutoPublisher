using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;

namespace ShortVideo.AutoPublisher.Infrastructure.Data;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    public AppDbContext(AppSettings settings)
    {
        var dbPath = settings.Database.FilePath;
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _disposed = true;
    }
}

/// <summary>
/// 数据库初始化器
/// </summary>
public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 初始化数据库表结构
    /// </summary>
    public async Task InitializeAsync()
    {
        using var connection = _context.CreateConnection();
        await connection.OpenAsync();

        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS VideoContents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT,
                Tags TEXT,
                FilePath TEXT NOT NULL,
                FileSize INTEGER DEFAULT 0,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                UpdatedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS CookieSessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Platform INTEGER NOT NULL,
                AccountName TEXT,
                AccountId TEXT,
                CookieData TEXT,
                IsValid INTEGER DEFAULT 1,
                IsDefault INTEGER DEFAULT 0,
                LastValidatedAt TEXT,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                UpdatedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS PublishTasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VideoId INTEGER NOT NULL,
                VideoTitle TEXT,
                Platform INTEGER NOT NULL,
                SessionId INTEGER NOT NULL,
                Status INTEGER DEFAULT 0,
                Progress INTEGER DEFAULT 0,
                RetryCount INTEGER DEFAULT 0,
                ErrorMessage TEXT,
                PublishedUrl TEXT,
                ScheduledTime TEXT,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                StartedAt TEXT,
                CompletedAt TEXT
            );
        ");

        _logger.LogInformation("数据库初始化完成");
    }
}
