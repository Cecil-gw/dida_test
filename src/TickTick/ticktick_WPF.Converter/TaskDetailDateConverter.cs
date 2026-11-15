using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskDetailDateConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 6)
		{
			DateTime? dateTime = values[0] as DateTime?;
			DateTime? due = values[1] as DateTime?;
			bool? isAllDay = values[2] as bool?;
			int? num = values[3] as int?;
			string text = values[4] as string;
			int valueOrDefault = (values[5] as int?).GetValueOrDefault();
			if (dateTime.HasValue)
			{
				return DateUtils.FormatDateString(dateTime.Value, due, isAllDay, num == 0);
			}
			if (valueOrDefault != 0)
			{
				return Utils.GetString("DateNotSet");
			}
			if (!(text == "NOTE"))
			{
				return Utils.GetString("DateAndReminder");
			}
			return Utils.GetString("SetReminder");
		}
		return Utils.GetString("DateAndReminder");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
