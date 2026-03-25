using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShortVideo.AutoPublisher.Core.Configuration;
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

    // 网络设置
    [ObservableProperty]
    private int _connectionTimeout;

    [ObservableProperty]
    private int _maxRetries;

    [ObservableProperty]
    private int _retryDelay;

    // 浏览器设置
    [ObservableProperty]
    private bool _headless;

    [ObservableProperty]
    private int _pageLoadTimeout;

    [ObservableProperty]
    private int _actionTimeout;

    // AI代理设置
    [ObservableProperty]
    private bool _enableAiAgent;

    [ObservableProperty]
    private int _maxConcurrentTasks;

    [ObservableProperty]
    private int _taskQueueSize;

    [ObservableProperty]
    private bool _enableAutoRetry;

    [ObservableProperty]
    private int _agentRetryCount;

    [ObservableProperty]
    private int _agentRetryDelay;

    [ObservableProperty]
    private bool _enableAntiDetection;

    // 主题
    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

    public SettingsViewModel(AppSettings settings, ISnackbarService snackbarService)
    {
        _settings = settings;
        _snackbarService = snackbarService;
        LoadSettings();
    }

    public void LoadSettings()
    {
        // 网络设置
        ConnectionTimeout = _settings.Network.ConnectionTimeoutSeconds;
        MaxRetries = _settings.Network.MaxRetries;
        RetryDelay = _settings.Network.InitialRetryDelaySeconds;

        // 浏览器设置
        Headless = _settings.Browser.Headless;
        PageLoadTimeout = _settings.Browser.PageLoadTimeoutSeconds;
        ActionTimeout = _settings.Browser.ActionTimeoutSeconds;

        // AI代理设置
        EnableAiAgent = _settings.Agent.EnableAiAgent;
        MaxConcurrentTasks = _settings.Agent.MaxConcurrentTasks;
        TaskQueueSize = _settings.Agent.TaskQueueSize;
        EnableAutoRetry = _settings.Agent.EnableAutoRetry;
        AgentRetryCount = _settings.Agent.RetryCount;
        AgentRetryDelay = _settings.Agent.RetryDelaySeconds;
        EnableAntiDetection = _settings.Agent.EnableAntiDetection;

        // 主题
        CurrentTheme = ApplicationThemeManager.GetAppTheme();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            // 网络设置
            _settings.Network.ConnectionTimeoutSeconds = ConnectionTimeout;
            _settings.Network.MaxRetries = MaxRetries;
            _settings.Network.InitialRetryDelaySeconds = RetryDelay;

            // 浏览器设置
            _settings.Browser.Headless = Headless;
            _settings.Browser.PageLoadTimeoutSeconds = PageLoadTimeout;
            _settings.Browser.ActionTimeoutSeconds = ActionTimeout;

            // AI代理设置
            _settings.Agent.EnableAiAgent = EnableAiAgent;
            _settings.Agent.MaxConcurrentTasks = MaxConcurrentTasks;
            _settings.Agent.TaskQueueSize = TaskQueueSize;
            _settings.Agent.EnableAutoRetry = EnableAutoRetry;
            _settings.Agent.RetryCount = AgentRetryCount;
            _settings.Agent.RetryDelaySeconds = AgentRetryDelay;
            _settings.Agent.EnableAntiDetection = EnableAntiDetection;

            _snackbarService.Show("成功", "设置已保存（重启后生效）", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
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
