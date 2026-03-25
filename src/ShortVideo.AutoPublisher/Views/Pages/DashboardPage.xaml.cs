using System.Windows.Controls;
using ShortVideo.AutoPublisher.ViewModels.Pages;

namespace ShortVideo.AutoPublisher.Views.Pages;

/// <summary>
/// DashboardPage.xaml 的交互逻辑
/// </summary>
public partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage(DashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        await ViewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
