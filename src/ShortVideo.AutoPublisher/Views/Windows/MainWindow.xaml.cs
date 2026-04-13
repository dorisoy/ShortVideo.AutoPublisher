using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Windows;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow(
        INavigationService navigationService,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();

        navigationService.SetNavigationControl(RootNavigation);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialog);

        RootNavigation.SetServiceProvider(serviceProvider);
    }
}
