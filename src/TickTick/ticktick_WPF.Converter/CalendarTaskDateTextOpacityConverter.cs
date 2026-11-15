using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class CalendarTaskDateTextOpacityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3 && values[0] != null && values[0] is double num)
		{
			if (values[1] is DateTime dateTime && values[2] is bool flag && dateTime < DateTime.Today && flag)
			{
				return 1;
			}
			return num;
		}
		return 0.6;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
