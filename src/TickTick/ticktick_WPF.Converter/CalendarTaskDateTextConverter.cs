using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CalendarTaskDateTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length >= 3)
		{
			string text = values[0] as string;
			if (values[1] != null && values[1] is bool flag && values[2] != null && values[2] is DateTime dateTime)
			{
				bool flag2 = default(bool);
				int num;
				if (values.Length == 5)
				{
					object obj = values[4];
					if (obj is bool)
					{
						flag2 = (bool)obj;
						num = 1;
					}
					else
					{
						num = 0;
					}
				}
				else
				{
					num = 0;
				}
				if (((uint)num & (flag2 ? 1u : 0u)) != 0)
				{
					DateTime? dateTime2 = values[3] as DateTime?;
					if (!dateTime2.HasValue)
					{
						dateTime2 = dateTime;
					}
					else if (flag)
					{
						dateTime2 = dateTime2.Value.AddSeconds(-1.0);
					}
					if (dateTime2.Value.Year < DateTime.Today.Year)
					{
						return DateUtils.FormatYearMonthDay(dateTime2.Value, DateFormatLen.Short);
					}
					try
					{
						return DateUtils.FormatMonthDay(dateTime2.Value, DateFormatLen.Long);
					}
					catch (Exception)
					{
						return dateTime2.Value.ToString("MM/dd", App.Ci);
					}
				}
				if (!flag)
				{
					if (string.IsNullOrEmpty(text))
					{
						return DateUtils.FormatHourMinuteText(dateTime);
					}
					return text + "\n" + DateUtils.FormatHourMinuteText(dateTime);
				}
				if (parameter != null && parameter.ToString() == "ShowAllDay")
				{
					return text + "\n" + DateUtils.FormatDateString(dateTime, null, true, useSmartDate: false);
				}
			}
			return text;
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
