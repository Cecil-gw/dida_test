using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CalendarReminderTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is List<int> minutes && values[1] is bool isAllDay)
		{
			return ReminderUtils.GetCalendarReminderText(minutes, isAllDay);
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
