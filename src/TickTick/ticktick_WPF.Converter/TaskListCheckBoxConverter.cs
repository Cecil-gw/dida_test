using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskListCheckBoxConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			return (int)value switch
			{
				0 => false, 
				1 => true, 
				2 => true, 
				_ => false, 
			};
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
