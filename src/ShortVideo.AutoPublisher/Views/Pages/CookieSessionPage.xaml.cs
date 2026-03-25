using System.Windows.Controls;
using ShortVideo.AutoPublisher.ViewModels.Pages;

namespace ShortVideo.AutoPublisher.Views.Pages;

/// <summary>
/// 账号管理页面
/// </summary>
public partial class CookieSessionPage : Page
{
    public CookieSessionPage(CookieSessionViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is CookieSessionViewModel viewModel)
        {
            await viewModel.LoadSessionsAsync();
        }
    }
}
