using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Converter;

public class TimePointTextMarginConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3 && values[0] is int num)
		{
			if (num == 0 || num / 60 == LocalSettings.Settings.CollapsedEnd)
			{
				return new Thickness(0.0, 0.0, 8.0, 0.0);
			}
			if (num / 60 == LocalSettings.Settings.CollapsedStart)
			{
				return new Thickness(0.0, -12.0, 8.0, 0.0);
			}
		}
		return new Thickness(0.0, -6.0, 8.0, 0.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
