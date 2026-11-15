using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskListPrintDateColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 5)
		{
			DateTime? startDate = values[0] as DateTime?;
			DateTime? dueDate = values[1] as DateTime?;
			bool? isAllDay = values[2] as bool?;
			object obj = values[3];
			if (obj is int && (int)obj != 0)
			{
				return ThemeUtil.GetAlphaColor("#191919", 56);
			}
			if (DateUtils.IsOutDated(startDate, dueDate, isAllDay))
			{
				return ThemeUtil.GetColor("OutDateColor");
			}
		}
		return ThemeUtil.GetAlphaColor("#191919", 56);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
