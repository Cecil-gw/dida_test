using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class NextIndicatorVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is DateTime dateTime && parameter != null)
		{
			bool flag = dateTime.Month == DateTime.Today.Month && dateTime.Year == DateTime.Today.Year;
			if (!flag && parameter.ToString() == "disable")
			{
				return Visibility.Visible;
			}
			if (flag && parameter.ToString() == "enable")
			{
				return Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
