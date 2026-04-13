using CommunityToolkit.Mvvm.ComponentModel;

namespace ShortVideo.AutoPublisher.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _applicationTitle = "ShortVideo AutoPublisher";
}
