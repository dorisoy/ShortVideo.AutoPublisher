using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;
using Wpf.Ui;

namespace ShortVideo.AutoPublisher.ViewModels.Pages;

/// <summary>
/// 发布任务视图模型
/// </summary>
public partial class PublishTaskViewModel : ViewModelBase
{
    private readonly PublishService _publishService;
    private readonly StatisticsService _statisticsService;
    private readonly ISnackbarService _snackbarService;

    [ObservableProperty]
    private ObservableCollection<PublishTask> _tasks = [];

    [ObservableProperty]
    private PublishTask? _selectedTask;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _runningCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _failedCount;

    public PublishTaskViewModel(
        PublishService publishService,
        StatisticsService statisticsService,
        ISnackbarService snackbarService)
    {
        _publishService = publishService;
        _statisticsService = statisticsService;
        _snackbarService = snackbarService;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            // 加载任务列表
            var tasks = await _publishService.GetAllTasksAsync();
            Tasks.Clear();
            foreach (var task in tasks)
            {
                Tasks.Add(task);
            }

            // 加载统计数据
            var counts = await _statisticsService.GetTaskStatusCountsAsync();
            PendingCount = counts.GetValueOrDefault(PublishTaskStatus.Pending, 0);
            RunningCount = counts.GetValueOrDefault(PublishTaskStatus.Running, 0);
            CompletedCount = counts.GetValueOrDefault(PublishTaskStatus.Completed, 0);
            FailedCount = counts.GetValueOrDefault(PublishTaskStatus.Failed, 0);
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

    public Task LoadTasksAsync() => LoadDataAsync();

    [RelayCommand]
    private async Task RetryTaskAsync(PublishTask? task)
    {
        if (task == null || !task.CanRetry) return;

        try
        {
            await _publishService.RetryTaskAsync(task.Id);
            _snackbarService.Show("成功", "任务已加入重试队列", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task CancelTaskAsync(PublishTask? task)
    {
        if (task == null || !task.CanCancel) return;

        try
        {
            await _publishService.CancelTaskAsync(task.Id);
            _snackbarService.Show("成功", "任务已取消", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private void ViewTaskDetail(PublishTask? task)
    {
        if (task == null) return;
        SelectedTask = task;
        // 可以在这里弹出详情对话框
        _snackbarService.Show("任务详情", $"视频: {task.VideoTitle}\n平台: {task.PlatformDisplayName}\n状态: {task.StatusDisplayName}", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(5));
    }
}
