using System.Windows;
using System.Windows.Controls;
using ShortVideo.AutoPublisher.Domain.Enums;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Dialogs;

/// <summary>
/// 添加账号对话框
/// </summary>
public partial class AddAccountDialog : FluentWindow
{
    public PlatformType SelectedPlatform
    {
        get
        {
            var selectedItem = PlatformComboBox.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag?.ToString();
            return tag switch
            {
                "Douyin" => PlatformType.Douyin,
                "Xiaohongshu" => PlatformType.Xiaohongshu,
                "Baijiahao" => PlatformType.Baijiahao,
                "WeixinChannel" => PlatformType.WeixinChannel,
                "Toutiao" => PlatformType.Toutiao,
                _ => PlatformType.Douyin
            };
        }
    }

    public string AccountName => AccountNameTextBox.Text;
    public string AccountId => AccountIdTextBox.Text;

    public AddAccountDialog()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AccountName))
        {
            System.Windows.MessageBox.Show("请输入账号名称", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
