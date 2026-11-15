using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class RatioBarWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is int val && values[1] is double num)
		{
			return num * (double)Math.Min(val, 100) / 100.0;
		}
		return 0;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
