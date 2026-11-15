using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class RepeatVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text)
		{
			if (string.IsNullOrEmpty(text) || text.Contains("FREQ=NONE") || text.Contains("FREQ = NONE") || text.Contains("CUSTOM"))
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
