using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ArrangeListHeightConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		double num = (double)values[0];
		if (values.Length == 2)
		{
			object obj = values[1];
			if (obj is Visibility && (Visibility)obj == Visibility.Visible)
			{
				num -= 40.0;
			}
		}
		return num;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
