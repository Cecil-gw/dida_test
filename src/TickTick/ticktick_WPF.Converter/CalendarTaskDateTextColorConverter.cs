using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CalendarTaskDateTextColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3 && values[0] != null && values[0] is Brush result)
		{
			if (values[1] is DateTime dateTime && values[2] is bool flag && dateTime < DateTime.Today && flag)
			{
				return ThemeUtil.GetColor("OutDateColor");
			}
			return result;
		}
		return ThemeUtil.GetColor("BaseColorOpacity40");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
