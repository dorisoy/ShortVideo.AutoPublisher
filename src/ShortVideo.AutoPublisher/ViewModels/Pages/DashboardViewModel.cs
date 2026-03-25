using System.Collections.ObjectModel;
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
}

/// <summary>
/// 仪表盘视图模型
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly StatisticsService _statisticsService;
    private readonly PublishService _publishService;
    private readonly INavigationService _navigationService;
    private readonly ISnackbarService _snackbarService;

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

    public DashboardViewModel(
        StatisticsService statisticsService,
        PublishService publishService,
        INavigationService navigationService,
        ISnackbarService snackbarService)
    {
        _statisticsService = statisticsService;
        _publishService = publishService;
        _navigationService = navigationService;
        _snackbarService = snackbarService;

        // 初始化平台状态
        foreach (PlatformType platform in Enum.GetValues<PlatformType>())
        {
            PlatformStatus.Add(new PlatformStatusItem
            {
                Platform = platform,
                Name = platform.GetDisplayName(),
                IsConnected = false
            });
        }
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

            LastUpdateTime = $"数据已更新 - {DateTime.Now:HH:mm:ss}";
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

    [RelayCommand]
    private void OneClickPublish()
    {
        _snackbarService.Show("提示", "一键发布功能开发中...", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
    }
}
