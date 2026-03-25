using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ShortVideo.AutoPublisher.Converters;

/// <summary>
/// 布尔值转颜色转换器
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue 
                ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))  // Green
                : new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)); // Red
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转状态文本转换器
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "有效" : "无效";
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔值转可见性转换器
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

/// <summary>
/// 反向布尔值转可见性转换器
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }
        return true;
    }
}

/// <summary>
/// 任务状态转颜色转换器
/// </summary>
public class TaskStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value?.ToString();
        return status switch
        {
            "Completed" => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), // Green
            "Running" => new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),   // Blue
            "Failed" => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),    // Red
            "Cancelled" => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)), // Gray
            "Pending" => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),   // Orange
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 空值转可见性转换器
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
