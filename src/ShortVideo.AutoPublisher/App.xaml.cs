using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ShortVideo.AutoPublisher.Core.Configuration;
using ShortVideo.AutoPublisher.Infrastructure.Data;
using ShortVideo.AutoPublisher.Infrastructure.Data.Repositories;
using ShortVideo.AutoPublisher.Infrastructure.Network;
using ShortVideo.AutoPublisher.OpenClaw.Abstractions;
using ShortVideo.AutoPublisher.OpenClaw.Agents;
using ShortVideo.AutoPublisher.OpenClaw.Browser;
using ShortVideo.AutoPublisher.OpenClaw.TaskScheduler;
using ShortVideo.AutoPublisher.Services;
using ShortVideo.AutoPublisher.ViewModels;
using ShortVideo.AutoPublisher.ViewModels.Pages;
using ShortVideo.AutoPublisher.Views.Pages;
using ShortVideo.AutoPublisher.Views.Windows;
using Wpf.Ui;

namespace ShortVideo.AutoPublisher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration((context, config) =>
        {
            config.SetBasePath(AppContext.BaseDirectory);
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices((context, services) =>
        {
            // 配置
            var appSettings = context.Configuration.Get<AppSettings>() ?? new AppSettings();
            services.AddSingleton(appSettings);

            // 配置 Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    Path.Combine(appSettings.Log.LogDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: appSettings.Log.RetainedFileDays)
                .CreateLogger();
            services.AddLogging(builder => builder.AddSerilog(dispose: true));

            // WPF-UI 服务
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();

            // 数据库
            services.AddSingleton<AppDbContext>();
            services.AddSingleton<DatabaseInitializer>();

            // 仓储
            services.AddSingleton<VideoContentRepository>();
            services.AddSingleton<PublishTaskRepository>();
            services.AddSingleton<CookieSessionRepository>();

            // 基础设施
            services.AddSingleton<NetworkMonitor>();

            // OpenClaw
            services.AddSingleton<BrowserManager>();
            services.AddSingleton<IAgentFactory, AgentFactory>();
            services.AddSingleton<PublishTaskScheduler>();

            // 服务
            services.AddSingleton<VideoContentService>();
            services.AddSingleton<PublishService>();
            services.AddSingleton<CookieSessionService>();
            services.AddSingleton<StatisticsService>();

            // 视图模型
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<VideoManageViewModel>();
            services.AddSingleton<PublishTaskViewModel>();
            services.AddSingleton<CookieSessionViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // 视图
            services.AddSingleton<MainWindow>();
            services.AddSingleton<DashboardPage>();
            services.AddSingleton<VideoManagePage>();
            services.AddSingleton<PublishTaskPage>();
            services.AddSingleton<CookieSessionPage>();
            services.AddSingleton<SettingsPage>();
        })
        .Build();

    /// <summary>
    /// 获取服务
    /// </summary>
    public static T GetService<T>() where T : class
    {
        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// 应用启动
    /// </summary>
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();

        // 初始化数据库
        var dbInitializer = GetService<DatabaseInitializer>();
        await dbInitializer.InitializeAsync();

        // 显示主窗口
        var mainWindow = GetService<MainWindow>();
        mainWindow.Show();
    }

    /// <summary>
    /// 应用退出
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        Log.CloseAndFlush();
    }

    /// <summary>
    /// 未处理异常
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "应用程序发生未处理异常");
        MessageBox.Show(
            $"发生错误: {e.Exception.Message}",
            "错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
