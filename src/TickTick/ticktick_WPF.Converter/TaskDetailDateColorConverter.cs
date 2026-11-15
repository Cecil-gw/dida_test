using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskDetailDateColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 4)
		{
			DateTime? dateTime = values[0] as DateTime?;
			DateTime? dueDate = values[1] as DateTime?;
			bool? isAllDay = values[2] as bool?;
			if (dateTime.HasValue && !Utils.IsEmptyDate(dateTime))
			{
				if (DateUtils.IsOutDated(dateTime, dueDate, isAllDay))
				{
					return ThemeUtil.GetColor("OutDateColor");
				}
				return ThemeUtil.GetColor("DateColorPrimary", ThemeUtil.GetColor("ThemeBlue"));
			}
			return ThemeUtil.GetColor("BaseColorOpacity60");
		}
		return ThemeUtil.GetColor("BaseColorOpacity60");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
