using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class HabitDateFilterMarginConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is double num && num > 400.0)
		{
			double num2 = (num - 54.0) / 14.0 - 30.0;
			return new Thickness(26.0 - num2, 0.0, 26.0 - num2, 8.0);
		}
		return new Thickness(26.0, 0.0, 26.0, 8.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
