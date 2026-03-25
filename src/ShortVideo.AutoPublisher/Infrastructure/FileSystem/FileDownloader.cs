using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Infrastructure.Network;

namespace ShortVideo.AutoPublisher.Infrastructure.FileSystem;

/// <summary>
/// 下载进度
/// </summary>
public class DownloadProgress
{
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double Progress => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
    public double SpeedBytesPerSecond { get; set; }
    
    public string FormattedProgress => $"{Progress:F1}%";
    public string FormattedSpeed => FormatBytes(SpeedBytesPerSecond) + "/s";
    
    private static string FormatBytes(double bytes) => bytes switch
    {
        >= 1073741824 => $"{bytes / 1073741824:F2} GB",
        >= 1048576 => $"{bytes / 1048576:F2} MB",
        >= 1024 => $"{bytes / 1024:F2} KB",
        _ => $"{bytes:F0} B"
    };
}

/// <summary>
/// 下载结果
/// </summary>
public class DownloadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public long DownloadedBytes { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 大文件下载器
/// </summary>
public class FileDownloader
{
    private readonly ILogger<FileDownloader> _logger;
    private readonly AppSettings _settings;
    private readonly NetworkMonitor _networkMonitor;
    private readonly HttpClient _httpClient;

    public FileDownloader(AppSettings settings, NetworkMonitor networkMonitor, ILogger<FileDownloader> logger)
    {
        _settings = settings;
        _networkMonitor = networkMonitor;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(settings.Network.ConnectionTimeoutSeconds)
        };
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    public async Task<DownloadResult> DownloadAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("开始下载: {Url}", url);

            // 确保目录存在
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 检查是否支持断点续传
            long existingLength = 0;
            var tempPath = destinationPath + ".tmp";
            
            if (_settings.Download.EnableResume && File.Exists(tempPath))
            {
                existingLength = new FileInfo(tempPath).Length;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (existingLength > 0)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            if (existingLength > 0 && response.StatusCode == System.Net.HttpStatusCode.PartialContent)
            {
                totalBytes += existingLength;
            }

            // 检查文件大小限制
            if (totalBytes > _settings.Download.MaxFileSizeBytes)
            {
                return new DownloadResult
                {
                    Success = false,
                    ErrorMessage = $"文件大小超过限制 ({totalBytes / 1048576:F2} MB > {_settings.Download.MaxFileSizeBytes / 1048576:F2} MB)"
                };
            }

            // 开始下载
            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(
                tempPath,
                existingLength > 0 ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                _settings.Download.BufferSize,
                true);

            var buffer = new byte[_settings.Download.BufferSize];
            var downloadedBytes = existingLength;
            var lastProgressReport = DateTime.Now;
            var lastDownloadedBytes = downloadedBytes;

            while (true)
            {
                // 检查网络状态
                if (!_networkMonitor.IsConnected)
                {
                    _logger.LogWarning("网络断开，等待恢复...");
                    await _networkMonitor.WaitForConnectivityAsync(ct);
                }

                var bytesRead = await contentStream.ReadAsync(buffer, ct);
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                downloadedBytes += bytesRead;

                // 报告进度
                var now = DateTime.Now;
                if ((now - lastProgressReport).TotalMilliseconds >= 500)
                {
                    var elapsed = (now - lastProgressReport).TotalSeconds;
                    var speed = (downloadedBytes - lastDownloadedBytes) / elapsed;
                    
                    progress?.Report(new DownloadProgress
                    {
                        TotalBytes = totalBytes,
                        DownloadedBytes = downloadedBytes,
                        SpeedBytesPerSecond = speed
                    });

                    lastProgressReport = now;
                    lastDownloadedBytes = downloadedBytes;
                }
            }

            // 下载完成，重命名文件
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }
            File.Move(tempPath, destinationPath);

            _logger.LogInformation("下载完成: {Path}, 大小: {Size} bytes", destinationPath, downloadedBytes);

            return new DownloadResult
            {
                Success = true,
                FilePath = destinationPath,
                DownloadedBytes = downloadedBytes
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("下载已取消: {Url}", url);
            return new DownloadResult
            {
                Success = false,
                ErrorMessage = "下载已取消"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载失败: {Url}", url);
            return new DownloadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
