using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class SubTaskTitleWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is double num && values[1] is double num2)
		{
			return Math.Max(0.0, num - num2 - 8.0);
		}
		return 0;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
