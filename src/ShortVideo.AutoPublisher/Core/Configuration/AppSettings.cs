namespace ShortVideo.AutoPublisher.Core.Configuration;

/// <summary>
/// 应用程序配置
/// </summary>
public class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public NetworkSettings Network { get; set; } = new();
    public BrowserSettings Browser { get; set; } = new();
    public DownloadSettings Download { get; set; } = new();
    public LogSettings Log { get; set; } = new();
    public AgentSettings Agent { get; set; } = new();
}

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseSettings
{
    public string FilePath { get; set; } = "Data/autopublisher.db";
}

/// <summary>
/// 网络配置
/// </summary>
public class NetworkSettings
{
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelaySeconds { get; set; } = 1;
    public int MaxRetryDelaySeconds { get; set; } = 30;
    public int NetworkCheckIntervalSeconds { get; set; } = 5;
}

/// <summary>
/// 浏览器配置
/// </summary>
public class BrowserSettings
{
    public bool Headless { get; set; } = false;
    public int PageLoadTimeoutSeconds { get; set; } = 60;
    public int ActionTimeoutSeconds { get; set; } = 30;
    public int DefaultDelayMs { get; set; } = 1000;
    public int MinDelayMs { get; set; } = 500;
    public int MaxDelayMs { get; set; } = 2000;
}

/// <summary>
/// 下载配置
/// </summary>
public class DownloadSettings
{
    public long MaxFileSizeBytes { get; set; } = 1073741824; // 1GB
    public int BufferSize { get; set; } = 81920;
    public bool EnableResume { get; set; } = true;
}

/// <summary>
/// 日志配置
/// </summary>
public class LogSettings
{
    public string LogDirectory { get; set; } = "Logs";
    public string MinimumLevel { get; set; } = "Information";
    public int RetainedFileDays { get; set; } = 30;
}

/// <summary>
/// AI代理配置
/// </summary>
public class AgentSettings
{
    public bool EnableAiAgent { get; set; } = true;
    public int MaxConcurrentTasks { get; set; } = 3;
    public int TaskQueueSize { get; set; } = 100;
    public bool EnableAutoRetry { get; set; } = true;
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public bool EnableAntiDetection { get; set; } = true;
}
