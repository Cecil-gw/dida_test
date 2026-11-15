using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class WeekTitleDayTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is DateTime { Day: var day })
		{
			return day.ToString();
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
