using ShortVideo.AutoPublisher.ViewModels.Pages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.LoadSettings();
        }

        // 为所有子控件附加滚轮事件处理
        AttachMouseWheelHandler(this);
    }

    private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // 事件已在 SettingsPage_Loaded 中处理
    }

    /// <summary>
    /// 递归为所有子控件附加 PreviewMouseWheel 事件
    /// </summary>
    private void AttachMouseWheelHandler(DependencyObject parent)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            // 如果是 UIElement，附加滚轮事件
            if (child is UIElement uiElement)
            {
                // 移除之前可能添加的处理程序，避免重复
                uiElement.PreviewMouseWheel -= OnChildPreviewMouseWheel;
                uiElement.PreviewMouseWheel += OnChildPreviewMouseWheel;
            }

            // 递归处理子控件
            AttachMouseWheelHandler(child);
        }
    }

    /// <summary>
    /// 子控件的滚轮事件处理 - 将事件冒泡给 ScrollViewer
    /// </summary>
    private void OnChildPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 将滚轮事件转发给 ScrollViewer
        if (MainScrollViewer != null && !e.Handled)
        {
            // 根据滚轮方向滚动 ScrollViewer
            if (e.Delta > 0)
            {
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset - 50);
            }
            else
            {
                MainScrollViewer.ScrollToVerticalOffset(MainScrollViewer.VerticalOffset + 50);
            }

            // 标记事件已处理，阻止子控件（如 NumberBox）消费滚轮事件
            e.Handled = true;
        }
    }
}
