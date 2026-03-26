using System.Windows;
using System.Windows.Controls;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Pages;

/// <summary>
/// 发布任务页面
/// </summary>
public partial class PublishTaskPage : Page
{
    private PublishTaskViewModel ViewModel => (PublishTaskViewModel)DataContext;
    private readonly IContentDialogService _dialogService;

    public PublishTaskPage(PublishTaskViewModel viewModel, IContentDialogService dialogService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _dialogService = dialogService;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadTasksAsync();
    }

    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is StatusFilterItem item)
        {
            ViewModel.FilterByStatusCommand.Execute(item);
        }
    }

    private async void ShowTaskDetailDialog_Click(object sender, RoutedEventArgs e)
    {
        var task = ViewModel.SelectedTask;
        if (task == null) return;

        var dialog = new ContentDialog
        {
            Title = $"任务详情 - #{task.Id}",
            CloseButtonText = "关闭",
            DefaultButton = ContentDialogButton.Close,
            Content = CreateTaskDetailContent(task)
        };

        await _dialogService.ShowAsync(dialog, CancellationToken.None);
    }

    private System.Windows.Controls.StackPanel CreateTaskDetailContent(PublishTask task)
    {
        var panel = new System.Windows.Controls.StackPanel { Width = 400 };

        // 视频信息
        AddDetailRow(panel, "视频标题", task.VideoTitle);
        AddDetailRow(panel, "发布平台", task.PlatformDisplayName);
        AddDetailRow(panel, "任务状态", task.StatusDisplayName);
        AddDetailRow(panel, "当前进度", $"{task.Progress}%");
        
        // 时间信息
        AddDetailRow(panel, "创建时间", task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        if (task.StartedAt.HasValue)
            AddDetailRow(panel, "开始时间", task.StartedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (task.CompletedAt.HasValue)
            AddDetailRow(panel, "完成时间", task.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        if (task.ScheduledTime.HasValue)
            AddDetailRow(panel, "计划发布", task.ScheduledTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));

        // 重试信息
        if (task.RetryCount > 0)
            AddDetailRow(panel, "重试次数", task.RetryCount.ToString());

        // 错误信息
        if (!string.IsNullOrEmpty(task.ErrorMessage))
        {
            panel.Children.Add(new System.Windows.Controls.TextBlock 
            { 
                Text = "错误信息", 
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 16, 0, 4) 
            });
            panel.Children.Add(new System.Windows.Controls.TextBlock 
            { 
                Text = task.ErrorMessage, 
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.Red
            });
        }

        // 发布链接
        if (!string.IsNullOrEmpty(task.PublishedUrl))
        {
            panel.Children.Add(new System.Windows.Controls.TextBlock 
            { 
                Text = "发布链接", 
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 16, 0, 4) 
            });
            var link = new System.Windows.Documents.Hyperlink();
            link.Inlines.Add(task.PublishedUrl);
            link.NavigateUri = new Uri(task.PublishedUrl);
            link.RequestNavigate += (s, e) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            };
            panel.Children.Add(new System.Windows.Controls.TextBlock(link));
        }

        return panel;
    }

    private void AddDetailRow(System.Windows.Controls.StackPanel panel, string label, string value)
    {
        var row = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 8) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelText = new System.Windows.Controls.TextBlock 
        { 
            Text = label + "：", 
            Foreground = System.Windows.Media.Brushes.Gray 
        };
        System.Windows.Controls.Grid.SetColumn(labelText, 0);

        var valueText = new System.Windows.Controls.TextBlock 
        { 
            Text = value,
            TextWrapping = TextWrapping.Wrap
        };
        System.Windows.Controls.Grid.SetColumn(valueText, 1);

        row.Children.Add(labelText);
        row.Children.Add(valueText);
        panel.Children.Add(row);
    }
}
