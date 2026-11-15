using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class HabitItemCheckinsMarginConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is double num)
		{
			double num2 = (num - 192.0) / 12.0;
			return new Thickness(38.0 - num2, 0.0, 14.0 - num2, 20.0);
		}
		return new Thickness(28.0, 0.0, 8.0, 20.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
