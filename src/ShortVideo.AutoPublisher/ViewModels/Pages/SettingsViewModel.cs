using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using ShortVideo.AutoPublisher.Core.Configuration;
using System.IO;
using System.Reflection;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace ShortVideo.AutoPublisher.ViewModels.Pages;

/// <summary>
/// 设置视图模型
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    private readonly ISnackbarService _snackbarService;

    #region 网络设置

    [ObservableProperty]
    private int _networkTimeoutSeconds = 30;

    [ObservableProperty]
    private int _retryCount = 3;

    [ObservableProperty]
    private int _retryDelaySeconds = 1;

    [ObservableProperty]
    private string _proxyAddress = string.Empty;

    #endregion

    #region 浏览器设置

    [ObservableProperty]
    private string _browserType = "Chromium";

    [ObservableProperty]
    private List<string> _browserTypes = ["Chromium", "Firefox", "WebKit"];

    [ObservableProperty]
    private bool _isHeadless;

    [ObservableProperty]
    private string _userDataDir = string.Empty;

    [ObservableProperty]
    private int _pageLoadTimeoutSeconds = 60;

    [ObservableProperty]
    private int _actionTimeoutSeconds = 30;

    [ObservableProperty]
    private int _maxConcurrentBrowsers = 1;

    #endregion

    #region AI代理设置

    [ObservableProperty]
    private bool _isAiAgentEnabled = true;

    [ObservableProperty]
    private int _taskTimeoutMinutes = 10;

    [ObservableProperty]
    private int _agentRetryCount = 3;

    [ObservableProperty]
    private int _maxConcurrentTasks = 1;

    [ObservableProperty]
    private bool _enableAntiDetection = true;

    #endregion

    #region 存储设置

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _downloadPath = string.Empty;

    #endregion

    #region 日志设置

    [ObservableProperty]
    private string _logLevel = "Information";

    [ObservableProperty]
    private List<string> _logLevels = ["Verbose", "Debug", "Information", "Warning", "Error", "Fatal"];

    [ObservableProperty]
    private string _logPath = string.Empty;

    [ObservableProperty]
    private int _logRetainedDays = 30;

    #endregion

    #region 关于

    [ObservableProperty]
    private string _appVersion = string.Empty;

    #endregion

    #region 主题

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

    #endregion

    public SettingsViewModel(AppSettings settings, ISnackbarService snackbarService)
    {
        _settings = settings;
        _snackbarService = snackbarService;
    }

    public void LoadSettings()
    {
        // 网络设置
        NetworkTimeoutSeconds = _settings.Network.ConnectionTimeoutSeconds;
        RetryCount = _settings.Network.MaxRetries;
        RetryDelaySeconds = _settings.Network.InitialRetryDelaySeconds;

        // 浏览器设置
        IsHeadless = _settings.Browser.Headless;
        PageLoadTimeoutSeconds = _settings.Browser.PageLoadTimeoutSeconds;
        ActionTimeoutSeconds = _settings.Browser.ActionTimeoutSeconds;
        MaxConcurrentBrowsers = 1;

        // AI代理设置
        IsAiAgentEnabled = _settings.Agent.EnableAiAgent;
        MaxConcurrentTasks = _settings.Agent.MaxConcurrentTasks;
        AgentRetryCount = _settings.Agent.RetryCount;
        TaskTimeoutMinutes = 10;
        EnableAntiDetection = _settings.Agent.EnableAntiDetection;

        // 存储设置
        DatabasePath = Path.GetFullPath(_settings.Database.FilePath);
        DownloadPath = _settings.Download.DownloadDirectory;

        // 日志设置
        LogLevel = _settings.Log.MinimumLevel;
        LogPath = Path.GetFullPath(_settings.Log.LogDirectory);
        LogRetainedDays = _settings.Log.RetainedFileDays;

        // 版本信息
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppVersion = $"版本 {version?.Major ?? 3}.{version?.Minor ?? 0}.{version?.Build ?? 1}";

        // 主题
        CurrentTheme = ApplicationThemeManager.GetAppTheme();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            // 网络设置
            _settings.Network.ConnectionTimeoutSeconds = NetworkTimeoutSeconds;
            _settings.Network.MaxRetries = RetryCount;
            _settings.Network.InitialRetryDelaySeconds = RetryDelaySeconds;

            // 浏览器设置
            _settings.Browser.Headless = IsHeadless;
            _settings.Browser.PageLoadTimeoutSeconds = PageLoadTimeoutSeconds;
            _settings.Browser.ActionTimeoutSeconds = ActionTimeoutSeconds;

            // AI代理设置
            _settings.Agent.EnableAiAgent = IsAiAgentEnabled;
            _settings.Agent.MaxConcurrentTasks = MaxConcurrentTasks;
            _settings.Agent.RetryCount = AgentRetryCount;
            _settings.Agent.EnableAntiDetection = EnableAntiDetection;

            // 日志设置
            _settings.Log.MinimumLevel = LogLevel;
            _settings.Log.RetainedFileDays = LogRetainedDays;

            _snackbarService.Show("成功", "设置已保存（重启后生效）", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        // 网络设置
        NetworkTimeoutSeconds = 30;
        RetryCount = 3;
        RetryDelaySeconds = 1;
        ProxyAddress = string.Empty;

        // 浏览器设置
        BrowserType = "Chromium";
        IsHeadless = false;
        PageLoadTimeoutSeconds = 60;
        ActionTimeoutSeconds = 30;
        MaxConcurrentBrowsers = 1;

        // AI代理设置
        IsAiAgentEnabled = true;
        TaskTimeoutMinutes = 10;
        AgentRetryCount = 3;
        MaxConcurrentTasks = 1;
        EnableAntiDetection = true;

        // 日志设置
        LogLevel = "Information";
        LogRetainedDays = 30;

        _snackbarService.Show("提示", "已重置为默认设置", Wpf.Ui.Controls.ControlAppearance.Info, null, TimeSpan.FromSeconds(3));
    }

    [RelayCommand]
    private void BrowseUserDataDir()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择用户数据目录"
        };

        if (dialog.ShowDialog() == true)
        {
            UserDataDir = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void BrowseDownloadPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择下载目录"
        };

        if (dialog.ShowDialog() == true)
        {
            DownloadPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void ChangeTheme(string theme)
    {
        var appTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            "Dark" => ApplicationTheme.Dark,
            _ => ApplicationTheme.Dark
        };

        ApplicationThemeManager.Apply(appTheme);
        CurrentTheme = appTheme;
    }
}
