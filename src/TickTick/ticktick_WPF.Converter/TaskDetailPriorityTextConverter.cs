using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskDetailPriorityTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int)
		{
			switch ((int)value)
			{
			case 0:
				return Utils.GetString("PriorityNull");
			case 1:
				return Utils.GetString("PriorityLow");
			case 3:
				return Utils.GetString("PriorityMedium");
			case 5:
				return Utils.GetString("PriorityHigh");
			}
		}
		return Utils.GetString("PriorityNull");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
