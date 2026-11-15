using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class KanbanTagMaxWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length != 3)
		{
			return 100;
		}
		if (values[1] == DependencyProperty.UnsetValue)
		{
			return 100;
		}
		double num = (double)values[0];
		int num2 = (int)values[1];
		double num3 = (double)values[2];
		return (num - num3) / (double)num2;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
