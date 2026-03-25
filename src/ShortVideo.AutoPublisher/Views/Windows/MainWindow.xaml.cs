using ShortVideo.AutoPublisher.ViewModels;
using ShortVideo.AutoPublisher.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Windows;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();

        // 设置导航服务
        navigationService.SetNavigationControl(RootNavigation);

        // 设置Snackbar服务
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);

        // 设置对话框服务
        contentDialogService.SetContentPresenter(RootContentDialog);

        // 导航到首页
        RootNavigation.Navigate(typeof(DashboardPage));
    }
}
