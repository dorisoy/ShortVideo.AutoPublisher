using System.Windows;
using System.Windows.Controls;
using ShortVideo.AutoPublisher.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Pages;

/// <summary>
/// 账号管理页面
/// </summary>
public partial class CookieSessionPage : Page
{
    public CookieSessionViewModel ViewModel { get; }
    private readonly IContentDialogService _dialogService;

    public CookieSessionPage(CookieSessionViewModel viewModel, IContentDialogService dialogService)
    {
        ViewModel = viewModel;
        _dialogService = dialogService;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadSessionsAsync();
    }

    private async void ShowAddAccountDialog_Click(object sender, RoutedEventArgs e)
    {
        // 重置表单
        ViewModel.ResetAddForm();

        var dialog = new ContentDialog
        {
            Title = "添加账号",
            PrimaryButtonText = "添加",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            Content = CreateAddAccountDialogContent()
        };

        var result = await _dialogService.ShowAsync(dialog, CancellationToken.None);
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ConfirmAddCommand.ExecuteAsync(null);
        }
    }

    private StackPanel CreateAddAccountDialogContent()
    {
        var panel = new StackPanel { Width = 400, Margin = new Thickness(0, 0, 0, 16) };

        // 平台选择
        var platformPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        platformPanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "平台", Margin = new Thickness(0, 0, 0, 8) });
        var platformComboBox = new ComboBox { Width = 400, ItemsSource = ViewModel.Platforms };
        platformComboBox.SetBinding(ComboBox.SelectedItemProperty, new System.Windows.Data.Binding("NewPlatform") { Source = ViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
        platformPanel.Children.Add(platformComboBox);
        panel.Children.Add(platformPanel);

        // 账号名称
        var namePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        namePanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "账号名称", Margin = new Thickness(0, 0, 0, 8) });
        var nameTextBox = new Wpf.Ui.Controls.TextBox { PlaceholderText = "输入账号名称或昵称" };
        nameTextBox.SetBinding(Wpf.Ui.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("NewAccountName") { Source = ViewModel, Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        namePanel.Children.Add(nameTextBox);
        panel.Children.Add(namePanel);

        // Cookie数据
        var cookiePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        cookiePanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Cookie数据", Margin = new Thickness(0, 0, 0, 8) });
        var cookieTextBox = new System.Windows.Controls.TextBox
        {
            Height = 120,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        cookieTextBox.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding("NewCookieData") { Source = ViewModel, Mode = System.Windows.Data.BindingMode.TwoWay, UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged });
        cookiePanel.Children.Add(cookieTextBox);
        panel.Children.Add(cookiePanel);

        // 设为默认
        var defaultCheckBox = new CheckBox { Content = "设为默认账号" };
        defaultCheckBox.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("NewIsDefault") { Source = ViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
        panel.Children.Add(defaultCheckBox);

        return panel;
    }
}
