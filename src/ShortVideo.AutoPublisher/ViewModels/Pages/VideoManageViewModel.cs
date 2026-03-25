using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Services;
using Wpf.Ui;

namespace ShortVideo.AutoPublisher.ViewModels.Pages;

/// <summary>
/// 视频管理视图模型
/// </summary>
public partial class VideoManageViewModel : ViewModelBase
{
    private readonly VideoContentService _videoService;
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

    public VideoManageViewModel(
        VideoContentService videoService,
        ISnackbarService snackbarService,
        IContentDialogService dialogService)
    {
        _videoService = videoService;
        _snackbarService = snackbarService;
        _dialogService = dialogService;
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
    private async Task DeleteVideoAsync(VideoContent? video)
    {
        if (video == null) return;

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
    private void PreviewVideo(VideoContent? video)
    {
        if (video == null || !File.Exists(video.FilePath)) return;

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
}
