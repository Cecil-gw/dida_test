using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskCellTitleWrapConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is double num && num > 48.0)
		{
			return TextWrapping.Wrap;
		}
		return TextWrapping.NoWrap;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
