using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Calendar;

namespace ticktick_WPF.Converter;

public class ToolTipTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			int num = int.Parse(value.ToString());
			string text = DateUtils.FormatHourMinute(DateTime.Today);
			string text2 = DateUtils.FormatHourMinute(DateTime.Today.AddHours(CalendarGeoHelper.GetStartHour(display: false)));
			if (num >= CalendarGeoHelper.GetEndHour(display: false) * 60)
			{
				text = DateUtils.FormatHourMinute(DateTime.Today.AddHours(CalendarGeoHelper.GetEndHour(display: false)));
				text2 = DateUtils.FormatHourMinute(DateTime.Today.AddHours(24.0));
			}
			if (CalendarGeoHelper.TopFolded)
			{
				return Utils.GetString("ClickToShow") + " " + text + " - " + text2;
			}
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
