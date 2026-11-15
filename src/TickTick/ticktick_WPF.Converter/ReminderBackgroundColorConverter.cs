using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class ReminderBackgroundColorConverter : IValueConverter
{
	public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null)
		{
			return ThemeUtil.GetColor("BaseColorOpacity0_10").ToString();
		}
		if (!(values is ICollection<TaskReminderModel> { Count: not 0 }))
		{
			return ThemeUtil.GetColor("BaseColorOpacity0_10").ToString();
		}
		return ThemeUtil.GetColor("BaseColorOpacity5").ToString();
	}

	public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
