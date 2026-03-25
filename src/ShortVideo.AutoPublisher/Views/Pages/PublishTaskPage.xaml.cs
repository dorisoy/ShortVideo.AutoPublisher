using System.Windows.Controls;
using ShortVideo.AutoPublisher.ViewModels.Pages;

namespace ShortVideo.AutoPublisher.Views.Pages;

/// <summary>
/// 发布任务页面
/// </summary>
public partial class PublishTaskPage : Page
{
    public PublishTaskPage(PublishTaskViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is PublishTaskViewModel viewModel)
        {
            await viewModel.LoadTasksAsync();
        }
    }
}
