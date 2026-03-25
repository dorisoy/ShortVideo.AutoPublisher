using System.Windows.Controls;
using ShortVideo.AutoPublisher.ViewModels.Pages;

namespace ShortVideo.AutoPublisher.Views.Pages;

/// <summary>
/// 设置页面
/// </summary>
public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.LoadSettings();
        }
    }
}
