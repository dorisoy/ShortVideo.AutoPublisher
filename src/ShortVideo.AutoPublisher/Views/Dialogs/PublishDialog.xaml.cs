using System;
using System.Collections.Generic;
using System.Windows;
using ShortVideo.AutoPublisher.Domain.Entities;
using ShortVideo.AutoPublisher.Domain.Enums;
using Wpf.Ui.Controls;

namespace ShortVideo.AutoPublisher.Views.Dialogs;

/// <summary>
/// 发布对话框
/// </summary>
public partial class PublishDialog : FluentWindow
{
    private readonly VideoContent _video;

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

    public DateTime? ScheduledTime
    {
        get
        {
            if (ImmediateRadio.IsChecked == true) return null;

            if (ScheduleDatePicker.SelectedDate == null) return null;

            if (TimeSpan.TryParse(ScheduleTimeTextBox.Text, out var time))
            {
                return ScheduleDatePicker.SelectedDate.Value.Date + time;
            }

            return ScheduleDatePicker.SelectedDate.Value;
        }
    }

    public PublishDialog(VideoContent video, IEnumerable<CookieSession>? sessions = null)
    {
        InitializeComponent();
        _video = video;
        
        VideoTitleText.Text = video.Title;

        // 更新平台登录状态
        if (sessions != null)
        {
            foreach (var session in sessions)
            {
                if (!session.IsValid) continue;

                switch (session.Platform)
                {
                    case PlatformType.Douyin:
                        DouyinStatusText.Text = $"已登录 ({session.AccountName})";
                        DouyinCheckBox.IsEnabled = true;
                        break;
                    case PlatformType.Xiaohongshu:
                        XiaohongshuStatusText.Text = $"已登录 ({session.AccountName})";
                        XiaohongshuCheckBox.IsEnabled = true;
                        break;
                    case PlatformType.Baijiahao:
                        BaijiahaoStatusText.Text = $"已登录 ({session.AccountName})";
                        BaijiahaoCheckBox.IsEnabled = true;
                        break;
                    case PlatformType.WeixinChannel:
                        WeixinStatusText.Text = $"已登录 ({session.AccountName})";
                        WeixinCheckBox.IsEnabled = true;
                        break;
                    case PlatformType.Toutiao:
                        ToutiaoStatusText.Text = $"已登录 ({session.AccountName})";
                        ToutiaoCheckBox.IsEnabled = true;
                        break;
                }
            }
        }

        ScheduledRadio.Checked += (s, e) => SchedulePanel.Visibility = Visibility.Visible;
        ScheduledRadio.Unchecked += (s, e) => SchedulePanel.Visibility = Visibility.Collapsed;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Publish_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedPlatforms.Count == 0)
        {
            System.Windows.MessageBox.Show("请至少选择一个发布平台", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
