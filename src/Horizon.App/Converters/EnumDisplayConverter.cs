using System.Globalization;
using System.Windows.Data;
using Horizon.App.Models;

namespace Horizon.App.Converters;

public sealed class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            ProjectStatus.Active => "进行中",
            ProjectStatus.OnHold => "暂停中",
            ProjectStatus.Completed => "已完成",
            WeeklyTaskStatus.Todo => "未开始",
            WeeklyTaskStatus.InProgress => "进行中",
            WeeklyTaskStatus.Done => "已完成",
            LongTermTaskStatus.Planned => "未开始",
            LongTermTaskStatus.Active => "进行中",
            LongTermTaskStatus.Paused => "进行中",
            LongTermTaskStatus.Completed => "已完成",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
