using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using ShortVideo.AutoPublisher.OpenClaw.Browser;
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
    private readonly BrowserManager _browserManager;

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

    // 浏览器登录状态
    [ObservableProperty]
    private bool _isBrowserLoginInProgress;

    [ObservableProperty]
    private string _browserLoginStatus = string.Empty;

    // 对话框显示控制
    [ObservableProperty]
    private bool _isAddDialogOpen = false;

    public Array Platforms => Enum.GetValues(typeof(PlatformType));

    public CookieSessionViewModel(
        CookieSessionService sessionService,
        ISnackbarService snackbarService,
        IContentDialogService dialogService,
        BrowserManager browserManager)
    {
        _sessionService = sessionService;
        _snackbarService = snackbarService;
        _dialogService = dialogService;
        _browserManager = browserManager;
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

    /// <summary>
    /// 在 UI 线程上更新属性，确保 WPF 绑定能正确接收变更通知
    /// </summary>
    private void DispatchToUI(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(action);
        }
        else
        {
            action();
        }
    }

    /// <summary>
    /// 通过浏览器登录获取Cookie
    /// </summary>
    [RelayCommand]
    private async Task LoginViaBrowserAsync()
    {
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        IBrowserContext? context = null;

        try
        {
            DispatchToUI(() =>
            {
                IsBrowserLoginInProgress = true;
                BrowserLoginStatus = "正在启动浏览器...";
            });

            var loginUrl = NewPlatform.GetLoginUrl();
            var platformName = NewPlatform.GetDisplayName();

            // 创建一个新的非无头浏览器实例（用户需要看到浏览器界面进行登录）
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args =
                [
                    "--disable-blink-features=AutomationControlled",
                    "--disable-infobars",
                    "--no-sandbox",
                    "--start-maximized"
                ]
            });

            context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
                Locale = "zh-CN",
                TimezoneId = "Asia/Shanghai",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            });

            // 注入反检测脚本
            await context.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh', 'en'] });
                window.chrome = { runtime: {} };
            ");

            var page = await context.NewPageAsync();

            DispatchToUI(() => BrowserLoginStatus = $"正在打开{platformName}登录页面...");
            await page.GotoAsync(loginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            DispatchToUI(() => BrowserLoginStatus = $"请在浏览器中完成{platformName}登录，登录成功后关闭浏览器窗口");

            // 在浏览器运行期间持续轮询保存最新 Cookie
            // 浏览器关闭后连接断开，此时无法再获取 Cookie，所以必须提前保存
            string lastCookieJson = string.Empty;
            int lastCookieCount = 0;

            // 监听浏览器断开连接事件，使用 TaskCompletionSource 来等待
            var disconnectedTcs = new TaskCompletionSource<bool>();
            browser.Disconnected += (_, _) => disconnectedTcs.TrySetResult(true);

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            try
            {
                while (!cts.Token.IsCancellationRequested && browser.IsConnected)
                {
                    try
                    {
                        var cookies = await context.CookiesAsync();
                        if (cookies.Count > 0)
                        {
                            lastCookieJson = System.Text.Json.JsonSerializer.Serialize(cookies);
                            lastCookieCount = cookies.Count;
                        }
                    }
                    catch
                    {
                        // 浏览器可能正在关闭，忽略异常
                        break;
                    }

                    // 等待 1 秒或浏览器断开，以较快者为准
                    var delayTask = Task.Delay(1000, cts.Token);
                    await Task.WhenAny(delayTask, disconnectedTcs.Task);

                    if (disconnectedTcs.Task.IsCompleted)
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // 超时，正常退出
            }

            // 使用最后成功保存的 Cookie，通过 Dispatcher 回到 UI 线程更新属性
            if (lastCookieCount > 0)
            {
                DispatchToUI(() =>
                {
                    NewCookieData = lastCookieJson;
                    BrowserLoginStatus = $"已成功获取 {lastCookieCount} 个 Cookie";
                });
                _snackbarService.Show("成功", $"已成功获取 {lastCookieCount} 个 Cookie", Wpf.Ui.Controls.ControlAppearance.Success, null, TimeSpan.FromSeconds(3));
            }
            else
            {
                DispatchToUI(() => BrowserLoginStatus = "未获取到Cookie，请确认已登录");
                _snackbarService.Show("提示", "未获取到Cookie，请确认已成功登录", Wpf.Ui.Controls.ControlAppearance.Caution, null, TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            DispatchToUI(() => BrowserLoginStatus = $"获取Cookie失败: {ex.Message}");
            _snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, TimeSpan.FromSeconds(3));
        }
        finally
        {
            if (context != null)
            {
                try { await context.CloseAsync(); } catch { }
            }
            if (browser != null)
            {
                try { await browser.CloseAsync(); } catch { }
            }
            playwright?.Dispose();

            DispatchToUI(() =>
            {
                IsBrowserLoginInProgress = false;
            });
            await Task.Delay(2000);
            DispatchToUI(() => BrowserLoginStatus = string.Empty);
        }
    }
}
