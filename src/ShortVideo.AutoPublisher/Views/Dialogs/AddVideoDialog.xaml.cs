using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using ShortVideo.AutoPublisher.Domain.Enums;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Dialogs;

/// <summary>
/// 添加视频对话框
/// </summary>
public partial class AddVideoDialog : FluentWindow
{
    public string VideoTitle => TitleTextBox.Text;
    public string VideoDescription => DescriptionTextBox.Text;
    public string VideoPath => VideoPathTextBox.Text;
    public string CoverPath => CoverPathTextBox.Text;
    public string Tags => TagsTextBox.Text;
    
    public List<PlatformType> SelectedPlatforms
    {
        get
        {
            var platforms = new List<PlatformType>();
            if (DouyinCheckBox.IsChecked == true) platforms.Add(PlatformType.Douyin);
            if (XiaohongshuCheckBox.IsChecked == true) platforms.Add(PlatformType.Xiaohongshu);
            if (BaijiahaoCheckBox.IsChecked == true) platforms.Add(PlatformType.Baijiahao);
            if (WeixinCheckBox.IsChecked == true) platforms.Add(PlatformType.WeixinChannel);
            if (ToutiaoCheckBox.IsChecked == true) platforms.Add(PlatformType.Toutiao);
            return platforms;
        }
    }

    public AddVideoDialog()
    {
        InitializeComponent();
    }

    private void BrowseVideo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择视频文件",
            Filter = "视频文件|*.mp4;*.avi;*.mov;*.wmv;*.flv;*.mkv|所有文件|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            VideoPathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseCover_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择封面图片",
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            CoverPathTextBox.Text = dialog.FileName;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(VideoTitle))
        {
            System.Windows.MessageBox.Show("请输入视频标题", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(VideoPath))
        {
            System.Windows.MessageBox.Show("请选择视频文件", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
