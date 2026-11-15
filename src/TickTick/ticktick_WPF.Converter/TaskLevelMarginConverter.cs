using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TaskLevelMarginConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is int num && parameter is string s && int.TryParse(s, out var result) && num >= 0)
		{
			bool valueOrDefault = values[1] as bool? == true;
			int num2 = Math.Min(4, num);
			return new Thickness(num2 * result + (valueOrDefault ? 10 : 0), 0.0, 0.0, 0.0);
		}
		return new Thickness(0.0, 0.0, 0.0, 0.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
