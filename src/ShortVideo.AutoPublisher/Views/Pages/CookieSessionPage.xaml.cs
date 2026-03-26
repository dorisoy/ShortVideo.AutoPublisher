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

        // Cookie数据区域
        var cookiePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };
        
        // Cookie标题和浏览器登录按钮在同一行
        var cookieHeaderPanel = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        cookieHeaderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cookieHeaderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        
        var cookieLabel = new System.Windows.Controls.TextBlock { Text = "Cookie数据", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(cookieLabel, 0);
        cookieHeaderPanel.Children.Add(cookieLabel);
        
        // 浏览器登录按钮
        var browserLoginBtn = new Wpf.Ui.Controls.Button
        {
            Content = "浏览器登录获取",
            Appearance = Wpf.Ui.Controls.ControlAppearance.Info,
            Icon = new Wpf.Ui.Controls.SymbolIcon { Symbol = Wpf.Ui.Controls.SymbolRegular.Globe24 }
        };
        browserLoginBtn.SetBinding(Wpf.Ui.Controls.Button.IsEnabledProperty, new System.Windows.Data.Binding("IsBrowserLoginInProgress") 
        { 
            Source = ViewModel, 
            Converter = new InverseBooleanConverter() 
        });
        browserLoginBtn.Click += async (s, e) => await ViewModel.LoginViaBrowserCommand.ExecuteAsync(null);
        Grid.SetColumn(browserLoginBtn, 1);
        cookieHeaderPanel.Children.Add(browserLoginBtn);
        
        cookiePanel.Children.Add(cookieHeaderPanel);
        
        // 登录状态提示
        var statusTextBlock = new System.Windows.Controls.TextBlock
        {
            Foreground = System.Windows.Media.Brushes.DeepSkyBlue,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        statusTextBlock.SetBinding(System.Windows.Controls.TextBlock.TextProperty, new System.Windows.Data.Binding("BrowserLoginStatus") { Source = ViewModel });
        statusTextBlock.SetBinding(System.Windows.Controls.TextBlock.VisibilityProperty, new System.Windows.Data.Binding("BrowserLoginStatus")
        {
            Source = ViewModel,
            Converter = new StringToVisibilityConverter()
        });
        cookiePanel.Children.Add(statusTextBlock);

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

/// <summary>
/// 布尔反转转换器
/// </summary>
public class InverseBooleanConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}

/// <summary>
/// 字符串到可见性转换器
/// </summary>
public class StringToVisibilityConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
