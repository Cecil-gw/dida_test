using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ThemeSelectionConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text)
		{
			string text2 = parameter?.ToString();
			if (text == text2)
			{
				return new Thickness(2.0, 2.0, 2.0, 2.0);
			}
			return new Thickness(0.0, 0.0, 0.0, 0.0);
		}
		return new Thickness(0.0, 0.0, 0.0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
