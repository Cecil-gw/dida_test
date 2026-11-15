using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Converter;

public class FilterEditVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			switch ((FilterMode)value)
			{
			case FilterMode.Normal:
				return (parameter == null || !(parameter.ToString() == "normal")) ? Visibility.Collapsed : Visibility.Visible;
			case FilterMode.Advanced:
				return (parameter != null && parameter.ToString() == "normal") ? Visibility.Collapsed : Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
