using System.Windows;
using System.Windows.Controls;
using ShortVideo.AutoPublisher.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Pages;

/// <summary>
/// VideoManagePage.xaml 的交互逻辑
/// </summary>
public partial class VideoManagePage : Page
{
    public VideoManageViewModel ViewModel { get; }
    private readonly IContentDialogService _dialogService;

    public VideoManagePage(VideoManageViewModel viewModel, IContentDialogService dialogService)
    {
        ViewModel = viewModel;
        _dialogService = dialogService;
        DataContext = this;
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataCommand.ExecuteAsync(null);
    }

    private async void ShowAddVideoDialog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "添加视频",
            PrimaryButtonText = "添加",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            Content = CreateAddVideoDialogContent()
        };

        var result = await _dialogService.ShowAsync(dialog, CancellationToken.None);
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.AddVideoCommand.ExecuteAsync(null);
        }
    }

    private System.Windows.Controls.StackPanel CreateAddVideoDialogContent()
    {
        var panel = new System.Windows.Controls.StackPanel { Width = 400, Margin = new Thickness(0, 0, 0, 16) };

        // 文件选择
        var filePanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        filePanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "选择视频文件", Margin = new Thickness(0, 0, 0, 8) });
        var fileRow = new System.Windows.Controls.StackPanel { Orientation = Orientation.Horizontal };
        var fileTextBox = new Wpf.Ui.Controls.TextBox
        {
            PlaceholderText = "拖拽视频文件到此处或点击下方按钮选择文件",
            IsReadOnly = true,
            Width = 320
        };
        fileTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ViewModel.NewVideoFilePath") { Source = this });
        var browseButton = new Wpf.Ui.Controls.Button { Content = "浏览文件...", Margin = new Thickness(8, 0, 0, 0) };
        browseButton.Click += (s, e) => ViewModel.BrowseFileCommand.Execute(null);
        fileRow.Children.Add(fileTextBox);
        fileRow.Children.Add(browseButton);
        filePanel.Children.Add(fileRow);
        panel.Children.Add(filePanel);

        // 标题
        var titlePanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        titlePanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "视频标题", Margin = new Thickness(0, 0, 0, 8) });
        var titleTextBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "请输入视频标题" };
        titleTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ViewModel.NewVideoTitle") { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay });
        titlePanel.Children.Add(titleTextBox);
        panel.Children.Add(titlePanel);

        // 描述
        var descPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        descPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "视频描述（可选）", Margin = new Thickness(0, 0, 0, 8) });
        var descTextBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "请输入视频描述", Height = 80, AcceptsReturn = true };
        descTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ViewModel.NewVideoDescription") { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay });
        descPanel.Children.Add(descTextBox);
        panel.Children.Add(descPanel);

        // 标签
        var tagPanel = new System.Windows.Controls.StackPanel();
        tagPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "标签（可选）", Margin = new Thickness(0, 0, 0, 8) });
        var tagTextBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "多个标签用逗号分隔" };
        tagTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ViewModel.NewVideoTags") { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay });
        tagPanel.Children.Add(tagTextBox);
        panel.Children.Add(tagPanel);

        return panel;
    }

    private async void ShowEditVideoDialog_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.EditingVideo == null) return;

        var dialog = new ContentDialog
        {
            Title = $"编辑视频 - {ViewModel.EditingVideo.Title}",
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            Content = CreateEditVideoDialogContent()
        };

        var result = await _dialogService.ShowAsync(dialog, CancellationToken.None);
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.SaveEditVideoCommand.ExecuteAsync(null);
        }
    }

    private System.Windows.Controls.StackPanel CreateEditVideoDialogContent()
    {
        var panel = new System.Windows.Controls.StackPanel { Width = 400, Margin = new Thickness(0, 0, 0, 16) };

        // 标题
        var titlePanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        titlePanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "视频标题", Margin = new Thickness(0, 0, 0, 8) });
        var titleTextBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "请输入视频标题" };
        titleTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ViewModel.EditVideoTitle") { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        titlePanel.Children.Add(titleTextBox);
        panel.Children.Add(titlePanel);

        // 描述
        var descPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        descPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "视频描述（可选）", Margin = new Thickness(0, 0, 0, 8) });
        var descTextBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "请输入视频描述", Height = 80, AcceptsReturn = true };
        descTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ViewModel.EditVideoDescription") { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        descPanel.Children.Add(descTextBox);
        panel.Children.Add(descPanel);

        // 标签
        var tagPanel = new System.Windows.Controls.StackPanel();
        tagPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "标签（可选）", Margin = new Thickness(0, 0, 0, 8) });
        var tagTextBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "多个标签用逗号分隔" };
        tagTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("ViewModel.EditVideoTags") { Source = this, Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        tagPanel.Children.Add(tagTextBox);
        panel.Children.Add(tagPanel);

        return panel;
    }

    private async void ShowPublishDialog_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.PublishingVideo == null) return;

        var dialog = new ContentDialog
        {
            Title = $"发布视频 - {ViewModel.PublishingVideo.Title}",
            PrimaryButtonText = "发布",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            Content = CreatePublishDialogContent()
        };

        var result = await _dialogService.ShowAsync(dialog, CancellationToken.None);
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ConfirmPublishVideoCommand.ExecuteAsync(null);
        }
    }

    private System.Windows.Controls.StackPanel CreatePublishDialogContent()
    {
        var panel = new System.Windows.Controls.StackPanel { Width = 350 };

        panel.Children.Add(new System.Windows.Controls.TextBlock 
        { 
            Text = "选择发布平台（可多选）", 
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16) 
        });

        // 平台选择列表
        foreach (var platform in ViewModel.PlatformSelections)
        {
            var checkBox = new System.Windows.Controls.CheckBox
            {
                Content = platform.Name,
                Margin = new Thickness(0, 0, 0, 8),
                DataContext = platform
            };
            checkBox.SetBinding(System.Windows.Controls.CheckBox.IsCheckedProperty, 
                new System.Windows.Data.Binding("IsSelected") { Mode = System.Windows.Data.BindingMode.TwoWay });
            panel.Children.Add(checkBox);
        }

        panel.Children.Add(new System.Windows.Controls.TextBlock 
        { 
            Text = "提示：确保已在账号管理中添加平台账号", 
            Foreground = System.Windows.Media.Brushes.Gray,
            FontSize = 12,
            Margin = new Thickness(0, 16, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        return panel;
    }
}
