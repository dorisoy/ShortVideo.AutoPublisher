using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _applicationTitle = "ShortVideo AutoPublisher - 短视频自动发布工具";

    [ObservableProperty]
    private ObservableCollection<object> _menuItems = [];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems = [];

    public MainWindowViewModel()
    {
        // 初始化菜单项（由MainWindow.xaml.cs设置）
    }
}
