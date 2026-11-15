using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class CalendarDateVisibilityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is double num && values[1] is Visibility visibility)
		{
			if (visibility == Visibility.Collapsed)
			{
				return (!(num >= 12.0)) ? Visibility.Collapsed : Visibility.Visible;
			}
			return (!(num >= 36.0)) ? Visibility.Collapsed : Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
