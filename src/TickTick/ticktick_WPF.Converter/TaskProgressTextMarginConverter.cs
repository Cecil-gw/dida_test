using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskProgressTextMarginConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2)
		{
			int num = (int)values[0];
			double num2 = (double)values[1];
			return new Thickness(num switch
			{
				0 => 5, 
				100 => (int)num2 - 40, 
				_ => (int)(TaskProgressUtils.GetPercentWidth(num, num2) - 5.0), 
			}, 2.0, 0.0, 4.0);
		}
		return new Thickness(0.0, 0.0, 0.0, 4.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
