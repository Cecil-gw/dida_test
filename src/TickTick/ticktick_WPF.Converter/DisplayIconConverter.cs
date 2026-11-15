using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class DisplayIconConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3)
		{
			object obj = values[2];
			if (obj is int && (int)obj != 0)
			{
				return Utils.GetGeometry("Ic.ProjectList.Completed");
			}
			if (values[0] is DisplayType displayType)
			{
				switch (displayType)
				{
				case DisplayType.CheckItem:
					return null;
				case DisplayType.Event:
					return Utils.GetIconData("IcCalendarEvent");
				case DisplayType.Agenda:
					return Utils.GetGeometry("Ic.TaskType.Schedule");
				}
			}
			if (values[1] is string text && text == "CHECKLIST")
			{
				return Utils.GetGeometry("Ic.TaskDetail.Checkitem");
			}
			return Utils.GetGeometry("Ic.TaskType.Uncompleted");
		}
		return Utils.GetGeometry("Ic.TaskType.Uncompleted");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
