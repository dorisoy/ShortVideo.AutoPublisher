using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;

namespace ShortVideo.AutoPublisher.Infrastructure.Network;

/// <summary>
/// 网络状态变更事件参数
/// </summary>
public class NetworkStatusChangedEventArgs : EventArgs
{
    public bool IsConnected { get; }
    public int LatencyMs { get; }

    public NetworkStatusChangedEventArgs(bool isConnected, int latencyMs = 0)
    {
        IsConnected = isConnected;
        LatencyMs = latencyMs;
    }
}

/// <summary>
/// 网络监控服务
/// </summary>
public class NetworkMonitor : IDisposable
{
    private readonly ILogger<NetworkMonitor> _logger;
    private readonly AppSettings _settings;
    private readonly Timer _checkTimer;
    private bool _isConnected;
    private bool _disposed;

    /// <summary>
    /// 网络状态变更事件
    /// </summary>
    public event EventHandler<NetworkStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// 当前是否已连接
    /// </summary>
    public bool IsConnected => _isConnected;

    public NetworkMonitor(AppSettings settings, ILogger<NetworkMonitor> logger)
    {
        _settings = settings;
        _logger = logger;
        _isConnected = true;
        
        _checkTimer = new Timer(
            CheckNetworkCallback,
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(settings.Network.NetworkCheckIntervalSeconds));
    }

    private async void CheckNetworkCallback(object? state)
    {
        try
        {
            var connected = await CheckConnectivityAsync();
            if (connected != _isConnected)
            {
                _isConnected = connected;
                var latency = connected ? await MeasureLatencyAsync() : 0;
                
                _logger.LogInformation("网络状态变更: {Status}", connected ? "已连接" : "已断开");
                StatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(connected, latency));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网络状态检查失败");
        }
    }

    /// <summary>
    /// 检查网络连通性
    /// </summary>
    public async Task<bool> CheckConnectivityAsync(CancellationToken ct = default)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("www.baidu.com", 3000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 测量网络延迟
    /// </summary>
    public async Task<int> MeasureLatencyAsync(string host = "www.baidu.com", CancellationToken ct = default)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 3000);
            return reply.Status == IPStatus.Success ? (int)reply.RoundtripTime : -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// 等待网络恢复
    /// </summary>
    public async Task WaitForConnectivityAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            if (await CheckConnectivityAsync(ct))
            {
                _isConnected = true;
                return;
            }

            _logger.LogWarning("等待网络恢复...");
            await Task.Delay(TimeSpan.FromSeconds(_settings.Network.NetworkCheckIntervalSeconds), ct);
        }
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    public void Start()
    {
        _checkTimer.Change(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_settings.Network.NetworkCheckIntervalSeconds));
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    public void Stop()
    {
        _checkTimer.Change(Timeout.Infinite, Timeout.Infinite);
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
            _checkTimer.Dispose();
        }

        _disposed = true;
    }
}
