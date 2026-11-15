using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskCellDateTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3)
		{
			bool? flag = values[0] as bool?;
			string text = string.Empty;
			if (values[1] is DateTime date)
			{
				if (flag ?? true)
				{
					return DateUtils.FormatShortDate(date);
				}
				text = DateUtils.FormatHourMinuteText(date);
			}
			if (values[1] is DateTime dateTime && values[2] is DateTime dateTime2 && dateTime2 != dateTime)
			{
				text = text + " - " + DateUtils.FormatHourMinuteText(dateTime2);
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
