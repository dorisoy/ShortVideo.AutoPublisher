using System.Globalization;
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
        if (value is bool b && b)
            return new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)); // green
        return new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8)); // red
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 布尔值转状态文字转换器
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return "有效";
        return "无效";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
