using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class RatioBarVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value.ToString().ToLower() == "real")
		{
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
