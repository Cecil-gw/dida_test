using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class DayTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return ((DateTime?)value)?.Day.ToString() ?? string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
