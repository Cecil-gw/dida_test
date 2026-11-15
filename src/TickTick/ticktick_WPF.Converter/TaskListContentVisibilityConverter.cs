using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskListContentVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null || value.ToString() == "")
		{
			return "Collapsed";
		}
		return "Visible";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
