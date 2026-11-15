using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskListDateTooltipConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 9)
		{
			DateTime? startDate = values[0] as DateTime?;
			DateTime? dueDate = values[1] as DateTime?;
			bool? isAllDay = values[2] as bool?;
			if (values[6] as bool? == true && DateUtils.IsOutDated(startDate, dueDate, isAllDay))
			{
				return string.Empty;
			}
			if (startDate.HasValue && !Utils.IsEmptyDate(startDate.Value))
			{
				return DateUtils.FormatListDateString(startDate.Value, dueDate, isAllDay, showDetail: true, values[8] as bool? == true);
			}
		}
		return string.Empty;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
