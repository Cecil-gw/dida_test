using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Converter;

public class TimePointTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 4 && values[0] is DateTime time)
		{
			TimeZoneInfo timeZone = (values[1] as TimeZoneInfo) ?? TimeZoneInfo.Local;
			bool valueOrDefault = values[2] as bool? == true;
			DateTime date = TimeZoneUtils.LocalToTargetTzTime(time, timeZone);
			if (valueOrDefault && (double)Math.Abs(DateTime.Now.Hour * 60 + DateTime.Now.Minute - date.Hour * 60 + date.Minute) / 60.0 * CalendarGeoHelper.HourHeight < 12.0)
			{
				return string.Empty;
			}
			if (date.Hour == 12 && date.Minute == 0)
			{
				return Utils.GetString("Noon");
			}
			if (LocalSettings.Settings.TimeFormat == "24Hour")
			{
				return DateUtils.FormatHourMinute(date, longTime: true);
			}
			if (!Utils.IsZhCn())
			{
				return DateUtils.GetAmPmTimeText(date);
			}
			return DateUtils.GetZhTime(date);
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
