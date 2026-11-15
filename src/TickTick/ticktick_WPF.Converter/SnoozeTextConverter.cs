using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class SnoozeTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && (DateTime)value > DateTime.Now)
		{
			DateTime date = (DateTime)value;
			if (Math.Abs((DateTime.Now.Date - date.Date).TotalDays - -1.0) <= 0.001)
			{
				return string.Format(Utils.GetString("PreviewSnoozeText"), Utils.GetString("Tomorrow"));
			}
			if (Math.Abs((DateTime.Now.Date - date.Date).TotalDays) <= 0.001)
			{
				return string.Format(Utils.GetString("PreviewSnoozeText"), DateUtils.FormatHourMinute(date));
			}
			return string.Format(Utils.GetString("PreviewSnoozeText"), DateUtils.FormatYearMonthDay(date, DateFormatLen.Middle) + " " + DateUtils.FormatHourMinute(date));
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
