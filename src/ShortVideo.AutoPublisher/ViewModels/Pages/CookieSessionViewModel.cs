using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.Services;
using Wpf.Ui;

namespace ShortVideo.AutoPublisher.ViewModels.Pages;

/// <summary>
/// Cookie会话视图模型
/// </summary>
public partial class CookieSessionViewModel : ViewModelBase
{
    private readonly CookieSessionService _sessionService;
    private readonly ISnackbarService _snackbarService;
    private readonly IContentDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<CookieSession> _sessions = [];

    [ObservableProperty]
    private CookieSession? _selectedSession;

    // 平台筛选
    [ObservableProperty]
    private string _selectedPlatformFilter = "全部";

    public ObservableCollection<string> PlatformFilters { get; } = ["全部", "抖音", "小红书", "百家号", "视频号", "头条"];

    // 筛选后的会话列表
    public IEnumerable<CookieSession> FilteredSessions => string.IsNullOrEmpty(SelectedPlatformFilter) || SelectedPlatformFilter == "全部"
        ? Sessions
        : Sessions.Where(s => GetPlatformDisplayName(s.Platform) == SelectedPlatformFilter);

    // 添加账号对话框字段
    [ObservableProperty]
    private PlatformType _newPlatform = PlatformType.Douyin;

    [ObservableProperty]
    private string _newAccountName = string.Empty;

    [ObservableProperty]
    private string _newCookieData = string.Empty;

    [ObservableProperty]
    private bool _newIsDefault = true;

    // 对话框显示控制
    [ObservableProperty]
    private bool _isAddDialogOpen = false;

    public Array Platforms => Enum.GetValues(typeof(PlatformType));

    public CookieSessionViewModel(
        CookieSessionService sessionService,
        ISnackbarService snackbarService,
        IContentDialogService dialogService)
    {
        _sessionService = sessionService;
        _snackbarService = snackbarService;
        _dialogService = dialogService;
    }

    partial void OnSelectedPlatformFilterChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredSessions));
    }

    /// <summary>
    /// 重置添加表单
    /// </summary>
    public void ResetAddForm()
    {
        NewPlatform = PlatformType.Douyin;
        NewAccountName = string.Empty;
        NewCookieData = string.Empty;
        NewIsDefault = true;
    }

    private static string GetPlatformDisplayName(PlatformType platform) => platform switch
    {
        PlatformType.Douyin => "抖音",
        PlatformType.Xiaohongshu => "小红书",
        PlatformType.Baijiahao => "百家号",
        PlatformType.WeixinChannel => "视频号",
        PlatformType.Toutiao => "头条",
        _ => platform.ToString()
    };

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var sessions = await _sessionService.GetAllAsync();
            Sessions.Clear();
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }
            OnPropertyChanged(nameof(FilteredSessions));
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

    public Task LoadSessionsAsync() => LoadDataAsync();

    /// <summary>
    /// 打开添加账号对话框
    /// </summary>
    [RelayCommand]
    private void ShowAddDialog()
    {
        // 重置表单
        NewPlatform = PlatformType.Douyin;
        NewAccountName = string.Empty;
        NewCookieData = string.Empty;
        NewIsDefault = true;
        IsAddDialogOpen = true;
    }

    /// <summary>
    /// 取消添加
    /// </summary>
    [RelayCommand]
    private void CancelAdd()
    {
        IsAddDialogOpen = false;
    }

    /// <summary>
    /// 确认添加账号
    /// </summary>
    [RelayCommand]
    private async Task ConfirmAddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName))
        {
            _snackbarService.Show("错误", "请填写账号名称", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        if (string.IsNullOrWhiteSpace(NewCookieData))
        {
            _snackbarService.Show("错误", "请填写Cookie数据", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            await _sessionService.AddAsync(NewPlatform, NewAccountName, NewCookieData, NewIsDefault);
            _snackbarService.Show("成功", "账号已添加", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));

            // 关闭对话框
            IsAddDialogOpen = false;

            // 清空表单
            NewAccountName = string.Empty;
            NewCookieData = string.Empty;
            NewIsDefault = true;

            // 刷新列表
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task SetDefaultAsync(CookieSession? session)
    {
        if (session == null) return;

        try
        {
            await _sessionService.SetDefaultAsync(session.Id);
            _snackbarService.Show("成功", "已设为默认账号", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task RefreshStatusAsync(CookieSession? session)
    {
        if (session == null) return;

        try
        {
            await _sessionService.RefreshStatusAsync(session.Id);
            _snackbarService.Show("成功", "状态已刷新", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(object? parameter)
    {
        if (parameter is not CookieSession session) return;

        try
        {
            await _sessionService.DeleteAsync(session.Id);
            Sessions.Remove(session);
            OnPropertyChanged(nameof(FilteredSessions));
            _snackbarService.Show("成功", "账号已删除", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task ValidateSessionAsync(object? parameter)
    {
        if (parameter is not CookieSession session) return;

        try
        {
            await _sessionService.RefreshStatusAsync(session.Id);
            _snackbarService.Show("成功", "验证完成", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private async Task UpdateCookieAsync(object? parameter)
    {
        if (parameter is not CookieSession session) return;

        // TODO: 弹出更新Cookie的对话框
        _snackbarService.Show("提示", "更新Cookie功能待实现", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
    }

    [RelayCommand]
    private async Task RefreshAllStatusAsync()
    {
        try
        {
            IsLoading = true;
            foreach (var session in Sessions)
            {
                await _sessionService.RefreshStatusAsync(session.Id);
            }
            await LoadDataAsync();
            _snackbarService.Show("成功", "所有账号状态已刷新", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
