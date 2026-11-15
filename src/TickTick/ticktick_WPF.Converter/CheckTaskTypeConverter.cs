using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class CheckTaskTypeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		List<string> list = (List<string>)value;
		string text = parameter as string;
		if (text == "All")
		{
			return list == null || list.Count == 0;
		}
		return list?.Contains(text);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
