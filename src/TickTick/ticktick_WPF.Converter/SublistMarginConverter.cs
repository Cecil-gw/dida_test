using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class SublistMarginConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int num && num > 0)
		{
			return new Thickness(15 + num * 15, 0.0, 0.0, 0.0);
		}
		if (value is int)
		{
			return new Thickness(15.0, 0.0, 0.0, 0.0);
		}
		return (value != null && (bool)value) ? new Thickness(30.0, 0.0, 0.0, 0.0) : new Thickness(15.0, 0.0, 0.0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
