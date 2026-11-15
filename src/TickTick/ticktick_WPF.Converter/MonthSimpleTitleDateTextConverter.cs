using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class MonthSimpleTitleDateTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is DateTime dateTime && parameter != null)
		{
			int num = int.Parse(parameter.ToString());
			return DateUtils.GetShortWeekTextByWeekDay((int)dateTime.AddDays(num).DayOfWeek);
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
