using System.Data;
using System.Data.SQLite;
using System.IO;
using ShortVideo.AutoPublisher.Core.Configuration;

namespace ShortVideo.AutoPublisher.Infrastructure.Data;

/// <summary>
/// SQLite数据库上下文
/// </summary>
public class AppDbContext : IDisposable
{
    private readonly string _connectionString;
    private SQLiteConnection? _connection;
    private bool _disposed;

    public AppDbContext(AppSettings settings)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, settings.Database.FilePath);
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        _connectionString = $"Data Source={dbPath};Version=3;";
    }

    /// <summary>
    /// 获取数据库连接
    /// </summary>
    public IDbConnection GetConnection()
    {
        if (_connection == null)
        {
            _connection = new SQLiteConnection(_connectionString);
        }

        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        return _connection;
    }

    /// <summary>
    /// 创建新的数据库连接
    /// </summary>
    public IDbConnection CreateConnection()
    {
        var connection = new SQLiteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        _disposed = true;
    }
}
