using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineArrangeTimeToText : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3 && values[2] is bool flag)
		{
			DateTime? dateTime = values[0] as DateTime?;
			DateTime? dateTime2 = values[1] as DateTime?;
			if (flag)
			{
				dateTime2 = dateTime2?.AddDays(-1.0);
			}
			DateTime? dateTime3 = dateTime2 ?? dateTime;
			if (dateTime3.HasValue)
			{
				DateTime valueOrDefault = dateTime3.GetValueOrDefault();
				return DateUtils.FormatDateCheckYear(valueOrDefault);
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
