using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.QuickAdd;

public class DetailIconMarginConverter1 : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is double num)
		{
			object obj = values[1];
			if (obj is bool)
			{
				if ((bool)obj)
				{
					return new Thickness(12.0, 0.0, 0.0, 0.0);
				}
				return (num < 280.0) ? new Thickness(5.0, 0.0, 0.0, 0.0) : new Thickness(8.0, 0.0, 0.0, 0.0);
			}
		}
		return new Thickness(8.0, 0.0, 0.0, 0.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
