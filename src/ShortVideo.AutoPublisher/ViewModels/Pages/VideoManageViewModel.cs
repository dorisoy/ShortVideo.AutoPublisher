using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;
using Wpf.Ui;

namespace ShortVideo.AutoPublisher.ViewModels.Pages;

/// <summary>
/// 视频管理视图模型
/// </summary>
public partial class VideoManageViewModel : ViewModelBase
{
    private readonly VideoContentService _videoService;
    private readonly PublishService _publishService;
    private readonly ISnackbarService _snackbarService;
    private readonly IContentDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<VideoContent> _videos = [];

    [ObservableProperty]
    private VideoContent? _selectedVideo;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    // 添加视频对话框字段
    [ObservableProperty]
    private string _newVideoTitle = string.Empty;

    [ObservableProperty]
    private string _newVideoDescription = string.Empty;

    [ObservableProperty]
    private string _newVideoTags = string.Empty;

    [ObservableProperty]
    private string _newVideoFilePath = string.Empty;

    // 编辑视频对话框字段
    [ObservableProperty]
    private VideoContent? _editingVideo;

    [ObservableProperty]
    private string _editVideoTitle = string.Empty;

    [ObservableProperty]
    private string _editVideoDescription = string.Empty;

    [ObservableProperty]
    private string _editVideoTags = string.Empty;

    // 发布对话框字段
    [ObservableProperty]
    private VideoContent? _publishingVideo;

    [ObservableProperty]
    private ObservableCollection<PlatformSelectionItem> _platformSelections = [];

    // 批量操作
    [ObservableProperty]
    private ObservableCollection<VideoContent> _selectedVideos = [];

    [ObservableProperty]
    private bool _isMultiSelectMode;

    public VideoManageViewModel(
        VideoContentService videoService,
        PublishService publishService,
        ISnackbarService snackbarService,
        IContentDialogService dialogService)
    {
        _videoService = videoService;
        _publishService = publishService;
        _snackbarService = snackbarService;
        _dialogService = dialogService;

        // 初始化平台选择列表
        InitializePlatformSelections();
    }

    private void InitializePlatformSelections()
    {
        PlatformSelections.Clear();
        foreach (PlatformType platform in Enum.GetValues<PlatformType>())
        {
            PlatformSelections.Add(new PlatformSelectionItem
            {
                Platform = platform,
                Name = platform.GetDisplayName(),
                IsSelected = false
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

            IEnumerable<VideoContent> videos;
            if (string.IsNullOrWhiteSpace(SearchKeyword))
            {
                videos = await _videoService.GetAllAsync();
            }
            else
            {
                videos = await _videoService.SearchAsync(SearchKeyword);
            }

            Videos.Clear();
            foreach (var video in videos)
            {
                Videos.Add(video);
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

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.avi;*.mov;*.mkv;*.wmv|所有文件|*.*",
            Title = "选择视频文件"
        };

        if (dialog.ShowDialog() == true)
        {
            NewVideoFilePath = dialog.FileName;
            if (string.IsNullOrEmpty(NewVideoTitle))
            {
                NewVideoTitle = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    [RelayCommand]
    private async Task AddVideoAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVideoFilePath) || string.IsNullOrWhiteSpace(NewVideoTitle))
        {
            _snackbarService.Show("错误", "请选择视频文件并填写标题", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            await _videoService.AddAsync(NewVideoFilePath, NewVideoTitle, NewVideoDescription, NewVideoTags);
            _snackbarService.Show("成功", "视频已添加", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            
            // 清空表单
            NewVideoFilePath = string.Empty;
            NewVideoTitle = string.Empty;
            NewVideoDescription = string.Empty;
            NewVideoTags = string.Empty;
            
            // 刷新列表
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task DeleteVideoAsync(object? parameter)
    {
        if (parameter is not VideoContent video) return;

        try
        {
            await _videoService.DeleteAsync(video.Id);
            Videos.Remove(video);
            _snackbarService.Show("成功", "视频已删除", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private void PreviewVideo(object? parameter)
    {
        if (parameter is not VideoContent video) return;
        if (!File.Exists(video.FilePath)) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = video.FilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    #region 编辑视频

    /// <summary>
    /// 开始编辑视频
    /// </summary>
    [RelayCommand]
    private void StartEditVideo(object? parameter)
    {
        if (parameter is not VideoContent video) return;
        EditingVideo = video;
        EditVideoTitle = video.Title;
        EditVideoDescription = video.Description ?? string.Empty;
        EditVideoTags = video.Tags ?? string.Empty;
    }

    /// <summary>
    /// 保存编辑的视频
    /// </summary>
    [RelayCommand]
    private async Task SaveEditVideoAsync()
    {
        if (EditingVideo == null || string.IsNullOrWhiteSpace(EditVideoTitle))
        {
            _snackbarService.Show("错误", "请填写视频标题", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            EditingVideo.Title = EditVideoTitle;
            EditingVideo.Description = EditVideoDescription;
            EditingVideo.Tags = EditVideoTags;
            await _videoService.UpdateAsync(EditingVideo);
            
            _snackbarService.Show("成功", "视频信息已更新", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    #endregion

    #region 发布视频

    /// <summary>
    /// 开始发布视频（打开平台选择对话框）
    /// </summary>
    [RelayCommand]
    private void StartPublishVideo(object? parameter)
    {
        if (parameter is not VideoContent video) return;
        PublishingVideo = video;
        
        // 重置平台选择
        foreach (var item in PlatformSelections)
        {
            item.IsSelected = false;
        }
    }

    /// <summary>
    /// 确认发布视频
    /// </summary>
    [RelayCommand]
    private async Task ConfirmPublishVideoAsync()
    {
        if (PublishingVideo == null)
        {
            _snackbarService.Show("错误", "请选择要发布的视频", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        var selectedPlatforms = PlatformSelections.Where(p => p.IsSelected).ToList();
        if (selectedPlatforms.Count == 0)
        {
            _snackbarService.Show("错误", "请至少选择一个发布平台", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
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
                    await _publishService.CreateTaskAsync(PublishingVideo.Id, platform.Platform);
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
                _snackbarService.Show("成功", $"已创建 {successCount} 个发布任务", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            }

            PublishingVideo = null;
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    #endregion

    #region 批量操作

    /// <summary>
    /// 切换多选模式
    /// </summary>
    [RelayCommand]
    private void ToggleMultiSelectMode()
    {
        IsMultiSelectMode = !IsMultiSelectMode;
        if (!IsMultiSelectMode)
        {
            SelectedVideos.Clear();
        }
    }

    /// <summary>
    /// 全选/取消全选
    /// </summary>
    [RelayCommand]
    private void SelectAllVideos()
    {
        if (SelectedVideos.Count == Videos.Count)
        {
            SelectedVideos.Clear();
        }
        else
        {
            SelectedVideos.Clear();
            foreach (var video in Videos)
            {
                SelectedVideos.Add(video);
            }
        }
    }

    /// <summary>
    /// 批量删除
    /// </summary>
    [RelayCommand]
    private async Task BatchDeleteVideosAsync()
    {
        if (SelectedVideos.Count == 0)
        {
            _snackbarService.Show("提示", "请先选择要删除的视频", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            var count = SelectedVideos.Count;
            foreach (var video in SelectedVideos.ToList())
            {
                await _videoService.DeleteAsync(video.Id);
                Videos.Remove(video);
            }
            SelectedVideos.Clear();
            _snackbarService.Show("成功", $"已删除 {count} 个视频", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    /// <summary>
    /// 批量发布（开始）
    /// </summary>
    [RelayCommand]
    private void StartBatchPublish()
    {
        if (SelectedVideos.Count == 0)
        {
            _snackbarService.Show("提示", "请先选择要发布的视频", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
            return;
        }
        
        // 重置平台选择
        foreach (var item in PlatformSelections)
        {
            item.IsSelected = false;
        }
    }

    /// <summary>
    /// 确认批量发布
    /// </summary>
    [RelayCommand]
    private async Task ConfirmBatchPublishAsync()
    {
        if (SelectedVideos.Count == 0)
        {
            _snackbarService.Show("错误", "请选择要发布的视频", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        var selectedPlatforms = PlatformSelections.Where(p => p.IsSelected).ToList();
        if (selectedPlatforms.Count == 0)
        {
            _snackbarService.Show("错误", "请至少选择一个发布平台", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            var totalTasks = 0;
            var failedCount = 0;

            foreach (var video in SelectedVideos.ToList())
            {
                foreach (var platform in selectedPlatforms)
                {
                    try
                    {
                        await _publishService.CreateTaskAsync(video.Id, platform.Platform);
                        totalTasks++;
                    }
                    catch
                    {
                        failedCount++;
                    }
                }
            }

            SelectedVideos.Clear();
            IsMultiSelectMode = false;

            if (failedCount > 0)
            {
                _snackbarService.Show("部分成功", $"成功创建 {totalTasks} 个任务，失败 {failedCount} 个", Wpf.Ui.Controls.ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
            }
            else
            {
                _snackbarService.Show("成功", $"已创建 {totalTasks} 个发布任务", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    #endregion
}

/// <summary>
/// 平台选择项
/// </summary>
public partial class PlatformSelectionItem : ObservableObject
{
    public PlatformType Platform { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
