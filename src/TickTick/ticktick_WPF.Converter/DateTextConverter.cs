using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class DateTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is DateTime { Year: var year } date)
		{
			if (year == DateTime.Now.Year)
			{
				return DateUtils.FormatMonthDay(date, DateFormatLen.Short);
			}
			return DateUtils.FormatYearMonthDay(date, DateFormatLen.Short);
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
