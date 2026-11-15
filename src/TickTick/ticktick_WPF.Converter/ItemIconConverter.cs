using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class ItemIconConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 4)
		{
			DisplayType type = DisplayType.Task;
			int status = 0;
			string kind = "TEXT";
			if (values[0] is DisplayType)
			{
				type = (DisplayType)values[0];
			}
			if (values[1] is string)
			{
				kind = values[1].ToString();
			}
			if (values[2] is int)
			{
				status = (int)values[2];
			}
			int valueOrDefault = (values[3] as int?).GetValueOrDefault();
			return ThemeUtil.GetTaskIconGeometry(type, status, kind, valueOrDefault);
		}
		return Utils.GetGeometry("Ic.TaskType.Uncompleted");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
