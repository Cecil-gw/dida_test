using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class BoolToEnabledConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is bool && values[1] is bool)
		{
			bool num = !(bool)values[0];
			bool flag = !(bool)values[1];
			return num || flag;
		}
		return false;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
