using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CalendarIconColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 4 && values[0] is string accountId && values[1] is string text && values[3] is ResourceDictionary dict)
		{
			BindCalendarModel defaultCalendar = SubscribeCalendarHelper.GetDefaultCalendar(accountId);
			if (defaultCalendar != null && defaultCalendar.Id != text)
			{
				return ThemeUtil.GetColor(dict, "PrimaryColor");
			}
		}
		return ThemeUtil.GetColor("BaseColorOpacity60");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
