using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class SelectedListConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		List<string> list = (List<string>)value;
		if (list != null && list.Count == 0)
		{
			return Utils.GetString("All");
		}
		if (parameter != null && parameter.ToString() == "Tag")
		{
			return TagCardViewModel.ToNormalDisplayText(list);
		}
		if (parameter != null && parameter.ToString() == "DueDate")
		{
			return DateCardViewModel.ToNormalDisplayText(list);
		}
		if (parameter != null && parameter.ToString() == "Assignee")
		{
			return AssigneeCardViewModel.ToNormalDisplayText(list);
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
