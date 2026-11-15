using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineAvatarVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 4)
		{
			string text = values[3] as string;
			if (string.IsNullOrEmpty(text) || text == "-1")
			{
				return Visibility.Collapsed;
			}
			double? num = (((values[0] as bool?) ?? true) ? (values[1] as double?) : (values[2] as double?));
			if (!num.HasValue || num < 70.0)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
