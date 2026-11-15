using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class MonthDayTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		DateTime? dateTime = (DateTime?)value;
		if (dateTime.HasValue)
		{
			return dateTime.Value.Day.ToString();
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
