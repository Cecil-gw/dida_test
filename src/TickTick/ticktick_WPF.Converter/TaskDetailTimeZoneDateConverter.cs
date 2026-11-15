using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskDetailTimeZoneDateConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		string text = "";
		if (values.Length == 4)
		{
			DateTime? dateTime = values[0] as DateTime?;
			DateTime? dateTime2 = values[1] as DateTime?;
			bool? isAllDay = values[2] as bool?;
			string text2 = values[3] as string;
			if (isAllDay != true && text2 != TimeZoneData.LocalTimeZoneModel?.TimeZoneName && text2 != null)
			{
				if (!string.IsNullOrEmpty(text2))
				{
					TimeZoneInfo timeZoneInfo = TimeZoneUtils.GetTimeZoneInfo(text2);
					double totalMinutes = (timeZoneInfo.BaseUtcOffset - TimeZoneInfo.Local.BaseUtcOffset).TotalMinutes;
					dateTime = dateTime?.AddMinutes(totalMinutes);
					dateTime2 = dateTime2?.AddMinutes(totalMinutes);
					text = timeZoneInfo.DisplayName;
				}
				if (dateTime.HasValue)
				{
					return DateUtils.FormatDateString(dateTime.Value, dateTime2, isAllDay, useSmartDate: false) + " " + text;
				}
			}
		}
		return text;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
