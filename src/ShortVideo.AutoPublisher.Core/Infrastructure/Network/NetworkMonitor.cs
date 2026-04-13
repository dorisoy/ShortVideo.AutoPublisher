using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using ShortVideo.AutoPublisher.Core.Configuration;

namespace ShortVideo.AutoPublisher.Infrastructure.Network;

/// <summary>
/// 网络状态变化事件参数
/// </summary>
public class NetworkStatusChangedEventArgs : EventArgs
{
    /// <summary>是否已连接</summary>
    public bool IsConnected { get; }

    /// <summary>延迟 (毫秒)</summary>
    public int LatencyMs { get; }

    /// <summary>
    /// 初始化网络状态变化事件参数
    /// </summary>
    public NetworkStatusChangedEventArgs(bool isConnected, int latencyMs)
    {
        IsConnected = isConnected;
        LatencyMs = latencyMs;
    }
}

/// <summary>
/// 网络监控器
/// </summary>
public class NetworkMonitor : IDisposable
{
    private readonly AppSettings _settings;
    private readonly ILogger<NetworkMonitor> _logger;
    private Timer? _checkTimer;
    private bool _isConnected = true;
    private bool _disposed;

    /// <summary>
    /// 网络状态变化事件
    /// </summary>
    public event EventHandler<NetworkStatusChangedEventArgs>? NetworkStatusChanged;

    /// <summary>
    /// 当前是否已连接
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// 初始化网络监控器
    /// </summary>
    public NetworkMonitor(AppSettings settings, ILogger<NetworkMonitor> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// 启动网络监控
    /// </summary>
    public void Start()
    {
        var intervalMs = (_settings.Network?.NetworkCheckIntervalSeconds ?? 30) * 1000;
        _checkTimer = new Timer(async _ => await CheckNetworkAsync(), null, 0, intervalMs);
    }

    /// <summary>
    /// 停止网络监控
    /// </summary>
    public void Stop()
    {
        _checkTimer?.Change(Timeout.Infinite, 0);
    }

    private async Task CheckNetworkAsync()
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 3000);
            var connected = reply.Status == IPStatus.Success;
            var latency = (int)reply.RoundtripTime;

            if (connected != _isConnected)
            {
                _isConnected = connected;
                NetworkStatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(connected, latency));
                _logger.LogInformation("网络状态变化: {Status}, 延迟: {Latency}ms",
                    connected ? "已连接" : "已断开", latency);
            }
        }
        catch (Exception ex)
        {
            if (_isConnected)
            {
                _isConnected = false;
                NetworkStatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(false, -1));
                _logger.LogWarning(ex, "网络检查失败");
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _checkTimer?.Dispose();
            }
            _disposed = true;
        }
    }
}
