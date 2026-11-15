using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskCellTopMarginConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		double val = 0.0;
		int num = 4;
		bool flag = parameter is string text && text == "False";
		if (value is double num2)
		{
			val = (num2 - (double)(flag ? 10 : 14)) / 2.0 - 1.0;
			num = (22 - (flag ? 10 : 14)) / 2;
		}
		return new Thickness(4.0, Math.Min(val, num), (!flag) ? 4 : 0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
