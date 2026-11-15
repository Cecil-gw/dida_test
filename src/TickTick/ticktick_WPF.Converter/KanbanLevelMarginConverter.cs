using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class KanbanLevelMarginConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int num && parameter is string s && int.TryParse(s, out var result) && num > 0)
		{
			int num2 = Math.Min(4, num);
			return new Thickness(16 + num2 * result, 4.0, 4.0, 4.0);
		}
		return new Thickness(0.0, 0.0, 0.0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
