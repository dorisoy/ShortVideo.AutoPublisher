using ShortVideo.AutoPublisher.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui.Controls;

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

            //Wpf.Ui.Controls.Button button= new Wpf.Ui.Controls.Button
            //{
            //    Content = "测试按钮",
            //    Icon =  new Wpf.Ui.Controls.SymbolIcon(SymbolRegular.Home24),
            //};
        }
    }
}
