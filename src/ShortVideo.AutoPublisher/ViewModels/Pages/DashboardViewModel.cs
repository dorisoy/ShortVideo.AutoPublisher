using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;
using Wpf.Ui;

namespace ShortVideo.AutoPublisher.ViewModels.Pages;

/// <summary>
/// 平台状态项
/// </summary>
public partial class PlatformStatusItem : ObservableObject
{
    [ObservableProperty]
    private PlatformType _platform;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isSelectedForPublish;
}

/// <summary>
/// 仪表盘视图模型
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly StatisticsService _statisticsService;
    private readonly PublishService _publishService;
    private readonly VideoContentService _videoService;
    private readonly INavigationService _navigationService;
    private readonly ISnackbarService _snackbarService;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty]
    private int _totalVideos;

    [ObservableProperty]
    private int _todayPublished;

    [ObservableProperty]
    private int _pendingTasks;

    [ObservableProperty]
    private int _failedTasks;

    [ObservableProperty]
    private int _connectedAccounts;

    [ObservableProperty]
    private int _expiredAccounts;

    [ObservableProperty]
    private ObservableCollection<PlatformStatusItem> _platformStatus = [];

    [ObservableProperty]
    private ObservableCollection<PublishTask> _recentTasks = [];

    [ObservableProperty]
    private string _lastUpdateTime = string.Empty;

    // 一键发布相关
    [ObservableProperty]
    private ObservableCollection<VideoContent> _availableVideos = [];

    [ObservableProperty]
    private VideoContent? _selectedVideoForPublish;

    [ObservableProperty]
    private bool _isAutoRefreshEnabled = true;

    public DashboardViewModel(
        StatisticsService statisticsService,
        PublishService publishService,
        VideoContentService videoService,
        INavigationService navigationService,
        ISnackbarService snackbarService)
    {
        _statisticsService = statisticsService;
        _publishService = publishService;
        _videoService = videoService;
        _navigationService = navigationService;
        _snackbarService = snackbarService;

        // 初始化平台状态
        foreach (PlatformType platform in Enum.GetValues<PlatformType>())
        {
            PlatformStatus.Add(new PlatformStatusItem
            {
                Platform = platform,
                Name = platform.GetDisplayName(),
                IsConnected = false,
                IsSelectedForPublish = false
            });
        }

        // 初始化自动刷新定时器
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _refreshTimer.Tick += async (s, e) =>
        {
            if (IsAutoRefreshEnabled)
            {
                await RefreshDataSilentlyAsync();
            }
        };
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var stats = await _statisticsService.GetDashboardStatisticsAsync();
            TotalVideos = stats.TotalVideos;
            TodayPublished = stats.TodayPublished;
            PendingTasks = stats.PendingTasks;
            FailedTasks = stats.FailedTasks;
            ConnectedAccounts = stats.ConnectedAccounts;
            ExpiredAccounts = stats.ExpiredAccounts;

            // 更新平台状态
            foreach (var item in PlatformStatus)
            {
                item.IsConnected = stats.PlatformStatus.GetValueOrDefault(item.Platform, false);
            }

            // 加载最近任务
            var recentTasks = await _publishService.GetRecentTasksAsync(5);
            RecentTasks.Clear();
            foreach (var task in recentTasks)
            {
                RecentTasks.Add(task);
            }

            // 加载可用视频
            var videos = await _videoService.GetAllAsync();
            AvailableVideos.Clear();
            foreach (var video in videos.Take(20))
            {
                AvailableVideos.Add(video);
            }

            LastUpdateTime = $"数据已更新 - {DateTime.Now:HH:mm:ss}";

            // 启动自动刷新
            if (!_refreshTimer.IsEnabled)
            {
                _refreshTimer.Start();
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshDataSilentlyAsync()
    {
        try
        {
            var stats = await _statisticsService.GetDashboardStatisticsAsync();
            TotalVideos = stats.TotalVideos;
            TodayPublished = stats.TodayPublished;
            PendingTasks = stats.PendingTasks;
            FailedTasks = stats.FailedTasks;
            ConnectedAccounts = stats.ConnectedAccounts;
            ExpiredAccounts = stats.ExpiredAccounts;

            foreach (var item in PlatformStatus)
            {
                item.IsConnected = stats.PlatformStatus.GetValueOrDefault(item.Platform, false);
            }

            var recentTasks = await _publishService.GetRecentTasksAsync(5);
            RecentTasks.Clear();
            foreach (var task in recentTasks)
            {
                RecentTasks.Add(task);
            }

            LastUpdateTime = $"数据已更新 - {DateTime.Now:HH:mm:ss}";
        }
        catch
        {
            // 静默刷新失败时不显示错误
        }
    }

    [RelayCommand]
    private void ToggleAutoRefresh()
    {
        IsAutoRefreshEnabled = !IsAutoRefreshEnabled;
        _snackbarService.Show("提示", 
            IsAutoRefreshEnabled ? "已启用自动刷新" : "已停用自动刷新", 
            Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(2));
    }

    [RelayCommand]
    private void NavigateToAddVideo()
    {
        _navigationService.Navigate(typeof(Views.Pages.VideoManagePage));
    }

    [RelayCommand]
    private void NavigateToCreateTask()
    {
        _navigationService.Navigate(typeof(Views.Pages.PublishTaskPage));
    }

    [RelayCommand]
    private void NavigateToManageAccount()
    {
        _navigationService.Navigate(typeof(Views.Pages.CookieSessionPage));
    }

    /// <summary>
    /// 开始一键发布（重置平台选择）
    /// </summary>
    [RelayCommand]
    private void StartOneClickPublish()
    {
        if (AvailableVideos.Count == 0)
        {
            _snackbarService.Show("提示", "请先添加视频到视频库", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }

        // 重置平台选择
        foreach (var item in PlatformStatus)
        {
            item.IsSelectedForPublish = item.IsConnected; // 默认选中已连接的平台
        }
    }

    /// <summary>
    /// 确认一键发布
    /// </summary>
    [RelayCommand]
    private async Task ConfirmOneClickPublishAsync()
    {
        if (SelectedVideoForPublish == null)
        {
            _snackbarService.Show("错误", "请选择要发布的视频", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        var selectedPlatforms = PlatformStatus.Where(p => p.IsSelectedForPublish && p.IsConnected).ToList();
        if (selectedPlatforms.Count == 0)
        {
            _snackbarService.Show("错误", "请至少选择一个已连接的平台", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            var successCount = 0;
            var failedPlatforms = new List<string>();

            foreach (var platform in selectedPlatforms)
            {
                try
                {
                    await _publishService.CreateTaskAsync(SelectedVideoForPublish.Id, platform.Platform);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedPlatforms.Add($"{platform.Name}: {ex.Message}");
                }
            }

            if (failedPlatforms.Count > 0)
            {
                _snackbarService.Show("部分成功", 
                    $"成功创建 {successCount} 个任务\n失败: {string.Join("; ", failedPlatforms)}", 
                    Wpf.Ui.Controls.ControlAppearance.Caution, null, TimeSpan.FromSeconds(5));
            }
            else
            {
                _snackbarService.Show("成功", $"已创建 {successCount} 个发布任务，调度器将自动执行", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            }

            // 刷新数据
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private void OneClickPublish()
    {
        // 已替换为 StartOneClickPublish 和 ConfirmOneClickPublish
        StartOneClickPublish();
    }

    /// <summary>
    /// 停止定时器
    /// </summary>
    public void StopRefreshTimer()
    {
        _refreshTimer.Stop();
    }
}
