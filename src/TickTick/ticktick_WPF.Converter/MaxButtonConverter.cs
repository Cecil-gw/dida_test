using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class MaxButtonConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (parameter.ToString() == "Max")
		{
			if ((WindowState)value != WindowState.Maximized)
			{
				return "Visible";
			}
			return "Collapsed";
		}
		if ((WindowState)value != WindowState.Maximized)
		{
			return "Collapsed";
		}
		return "Visible";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
