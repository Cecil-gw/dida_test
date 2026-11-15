using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CalendarDetailDateConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3)
		{
			DateTime? dateTime = values[0] as DateTime?;
			DateTime? dateTime2 = values[1] as DateTime?;
			bool flag = (values[2] as bool?) ?? true;
			if (dateTime.HasValue)
			{
				dateTime2 = dateTime2?.AddDays(flag ? (-1) : 0) ?? dateTime.Value;
				return DateUtils.FormatCalendarDateString(dateTime.Value, dateTime2.Value, flag);
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
