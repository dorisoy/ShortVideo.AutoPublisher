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

    [ObservableProperty]
    private ObservableCollection<CookieSession> _sessions = [];

    [ObservableProperty]
    private CookieSession? _selectedSession;

    // 添加账号对话框字段
    [ObservableProperty]
    private PlatformType _newPlatform = PlatformType.Douyin;

    [ObservableProperty]
    private string _newAccountName = string.Empty;

    [ObservableProperty]
    private string _newCookieData = string.Empty;

    [ObservableProperty]
    private bool _newIsDefault = true;

    public Array Platforms => Enum.GetValues(typeof(PlatformType));

    public CookieSessionViewModel(
        CookieSessionService sessionService,
        ISnackbarService snackbarService)
    {
        _sessionService = sessionService;
        _snackbarService = snackbarService;
    }

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

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName) || string.IsNullOrWhiteSpace(NewCookieData))
        {
            _snackbarService.Show("错误", "请填写账号名称和Cookie数据", Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
            return;
        }

        try
        {
            await _sessionService.AddAsync(NewPlatform, NewAccountName, NewCookieData, NewIsDefault);
            _snackbarService.Show("成功", "账号已添加", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));

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
    private async Task DeleteAccountAsync(CookieSession? session)
    {
        if (session == null) return;

        try
        {
            await _sessionService.DeleteAsync(session.Id);
            Sessions.Remove(session);
            _snackbarService.Show("成功", "账号已删除", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
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
