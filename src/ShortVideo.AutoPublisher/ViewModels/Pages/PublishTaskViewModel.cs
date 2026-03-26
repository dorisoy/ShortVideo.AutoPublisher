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

    private List<PublishTask> _allTasks = [];

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

    // 筛选和搜索
    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private PublishTaskStatus? _filterStatus;

    [ObservableProperty]
    private ObservableCollection<StatusFilterItem> _statusFilters = [];

    // 批量操作
    [ObservableProperty]
    private ObservableCollection<PublishTask> _selectedTasks = [];

    [ObservableProperty]
    private bool _isMultiSelectMode;

    public PublishTaskViewModel(
        PublishService publishService,
        StatisticsService statisticsService,
        ISnackbarService snackbarService)
    {
        _publishService = publishService;
        _statisticsService = statisticsService;
        _snackbarService = snackbarService;

        // 初始化状态筛选列表
        StatusFilters.Add(new StatusFilterItem { Status = null, Name = "全部" });
        foreach (PublishTaskStatus status in Enum.GetValues<PublishTaskStatus>())
        {
            StatusFilters.Add(new StatusFilterItem { Status = status, Name = status.GetDisplayName() });
        }
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            // 加载所有任务
            _allTasks = (await _publishService.GetAllTasksAsync()).ToList();
            
            // 应用筛选
            ApplyFilter();

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

    private void ApplyFilter()
    {
        var filtered = _allTasks.AsEnumerable();

        // 状态筛选
        if (FilterStatus.HasValue)
        {
            filtered = filtered.Where(t => t.Status == FilterStatus.Value);
        }

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            var keyword = SearchKeyword.ToLower();
            filtered = filtered.Where(t => 
                t.VideoTitle.ToLower().Contains(keyword) ||
                t.PlatformDisplayName.ToLower().Contains(keyword));
        }

        Tasks.Clear();
        foreach (var task in filtered)
        {
            Tasks.Add(task);
        }
    }

    [RelayCommand]
    private void Search()
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void FilterByStatus(object? parameter)
    {
        if (parameter is StatusFilterItem item)
        {
            FilterStatus = item.Status;
            ApplyFilter();
        }
    }

    public Task LoadTasksAsync() => LoadDataAsync();

    [RelayCommand]
    private async Task RetryTaskAsync(object? parameter)
    {
        if (parameter is not PublishTask task || !task.CanRetry) return;

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
    private async Task CancelTaskAsync(object? parameter)
    {
        if (parameter is not PublishTask task || !task.CanCancel) return;

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
    private void ViewTaskDetail(object? parameter)
    {
        if (parameter is not PublishTask task) return;
        SelectedTask = task;
    }

    #region 删除任务

    [RelayCommand]
    private async Task DeleteTaskAsync(object? parameter)
    {
        if (parameter is not PublishTask task) return;

        // 只能删除已完成、失败或已取消的任务
        if (!task.Status.IsTerminal())
        {
            _snackbarService.Show("提示", "只能删除已完成、失败或已取消的任务", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            await _publishService.DeleteTaskAsync(task.Id);
            Tasks.Remove(task);
            _allTasks.Remove(task);
            _snackbarService.Show("成功", "任务已删除", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    #endregion

    #region 批量操作

    [RelayCommand]
    private void ToggleMultiSelectMode()
    {
        IsMultiSelectMode = !IsMultiSelectMode;
        if (!IsMultiSelectMode)
        {
            SelectedTasks.Clear();
        }
    }

    [RelayCommand]
    private async Task BatchDeleteTasksAsync()
    {
        if (SelectedTasks.Count == 0)
        {
            _snackbarService.Show("提示", "请先选择要删除的任务", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }

        var deletableTasks = SelectedTasks.Where(t => t.Status.IsTerminal()).ToList();
        if (deletableTasks.Count == 0)
        {
            _snackbarService.Show("提示", "只能删除已完成、失败或已取消的任务", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            foreach (var task in deletableTasks)
            {
                await _publishService.DeleteTaskAsync(task.Id);
                Tasks.Remove(task);
                _allTasks.Remove(task);
            }
            SelectedTasks.Clear();
            _snackbarService.Show("成功", $"已删除 {deletableTasks.Count} 个任务", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task BatchRetryTasksAsync()
    {
        if (SelectedTasks.Count == 0)
        {
            _snackbarService.Show("提示", "请先选择要重试的任务", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }

        var retryableTasks = SelectedTasks.Where(t => t.CanRetry).ToList();
        if (retryableTasks.Count == 0)
        {
            _snackbarService.Show("提示", "没有可重试的任务", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            foreach (var task in retryableTasks)
            {
                await _publishService.RetryTaskAsync(task.Id);
            }
            SelectedTasks.Clear();
            await LoadDataAsync();
            _snackbarService.Show("成功", $"已将 {retryableTasks.Count} 个任务加入重试队列", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    #endregion
}

/// <summary>
/// 状态筛选项
/// </summary>
public class StatusFilterItem
{
    public PublishTaskStatus? Status { get; set; }
    public string Name { get; set; } = string.Empty;
}
