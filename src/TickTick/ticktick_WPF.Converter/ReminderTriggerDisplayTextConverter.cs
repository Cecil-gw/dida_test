using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class ReminderTriggerDisplayTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || (values.Length != 2 && values.Length != 1))
		{
			return string.Empty;
		}
		ICollection<string> triggers = values[0] as ICollection<string>;
		bool isAllday = true;
		if (values.Length == 2)
		{
			isAllday = values[1] as bool? == true;
		}
		return ReminderUtils.GetReminderTriggerListDisplayText(triggers, isAllday, Utils.GetString("none"));
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
