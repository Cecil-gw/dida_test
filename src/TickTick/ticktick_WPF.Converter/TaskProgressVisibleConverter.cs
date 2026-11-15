using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskProgressVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 4 && values[0] is int num && values[1] is bool flag && values[2] is bool flag2 && values[3] is string text)
		{
			if (text == "NOTE")
			{
				return Visibility.Collapsed;
			}
			if (num != 0)
			{
				return Visibility.Collapsed;
			}
			if (!flag2)
			{
				return Visibility.Collapsed;
			}
			if (flag)
			{
				return Visibility.Collapsed;
			}
		}
		return Visibility.Visible;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
