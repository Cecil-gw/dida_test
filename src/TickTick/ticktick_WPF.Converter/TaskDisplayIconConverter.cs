using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskDisplayIconConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3)
		{
			if (values[0] is int num)
			{
				switch (num)
				{
				case -1:
					return Utils.GetGeometry("Ic.TaskType.Abandon");
				default:
					return Utils.GetGeometry("Ic.TaskType.Completed");
				case 0:
					break;
				}
			}
			if (values[2] is string value && !string.IsNullOrEmpty(value))
			{
				return Utils.GetGeometry("Ic.TaskType.Schedule");
			}
			if (values[1] is string text && text == "CHECKLIST")
			{
				return Utils.GetGeometry("Ic.TaskType.CheckitemTitle");
			}
		}
		return Utils.GetGeometry("Ic.TaskType.Uncompleted");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
