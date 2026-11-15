using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class PriorityCheckConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (parameter != null)
		{
			int item = int.Parse(parameter.ToString());
			List<int> list = (List<int>)value;
			if (list != null)
			{
				return list.Contains(item);
			}
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
