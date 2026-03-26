using ShortVideo.AutoPublisher.ViewModels.Pages;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

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

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.LoadSettings();
        }
    }

    /// <summary>
    /// Grid 级别的滚轮事件处理 - 确保整个页面区域都能响应滚动
    /// </summary>
    private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (MainScrollViewer != null)
        {
            // 手动滚动 ScrollViewer
            MainScrollViewer.ScrollToVerticalOffset(
                MainScrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }

    /// <summary>
    /// 处理超链接导航
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch
        {
            // 忽略打开链接失败的错误
        }
        e.Handled = true;
    }
}
