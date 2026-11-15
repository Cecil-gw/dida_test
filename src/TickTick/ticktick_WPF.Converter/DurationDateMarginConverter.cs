using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class DurationDateMarginConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool)
		{
			return ((bool)value) ? new Thickness(4.0, 0.0, 12.0, 0.0) : new Thickness(4.0, 0.0, 8.0, 0.0);
		}
		return new Thickness(4.0, 0.0, 12.0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
