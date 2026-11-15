using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskDetailDateMaxWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is double num && values[1] is Visibility visibility)
		{
			if (num < 50.0)
			{
				return 250;
			}
			if (visibility == Visibility.Visible)
			{
				return num - 50.0;
			}
			return num - 30.0;
		}
		return 250;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
