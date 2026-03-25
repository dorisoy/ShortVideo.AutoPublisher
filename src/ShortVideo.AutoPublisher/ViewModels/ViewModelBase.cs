using CommunityToolkit.Mvvm.ComponentModel;

namespace ShortVideo.AutoPublisher.ViewModels;

/// <summary>
/// 视图模型基类
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// 显示错误消息
    /// </summary>
    protected void ShowError(string message)
    {
        ErrorMessage = message;
    }

    /// <summary>
    /// 清除错误消息
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
    }
}
