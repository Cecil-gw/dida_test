using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class CheckAssigneeAllConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		List<string> list = (List<string>)value;
		return list == null || list.Count == 0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
