using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Models;

namespace ticktick_WPF.Converter;

public class HabitProgressConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is HabitCheckInModel habitCheckInModel)
		{
			return Math.Min((int)(habitCheckInModel.Value / habitCheckInModel.Goal * 100.0), 100);
		}
		return 0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
