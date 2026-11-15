using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class FilterConditionTitleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			CondType condType = (CondType)value;
			string arg = string.Empty;
			switch (condType)
			{
			case CondType.Lists:
				arg = Utils.GetString("lists");
				break;
			case CondType.Tag:
				arg = Utils.GetString("tag");
				break;
			case CondType.Date:
				arg = Utils.GetString("date");
				break;
			case CondType.Assignee:
				arg = Utils.GetString("assignee");
				break;
			case CondType.Priority:
				arg = Utils.GetString("priority");
				break;
			case CondType.TaskType:
				arg = Utils.GetString("TaskType");
				break;
			}
			return string.Format(Utils.GetString("LogicOf"), arg);
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
