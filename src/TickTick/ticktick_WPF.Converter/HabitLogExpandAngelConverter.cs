using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class HabitLogExpandAngelConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int num && num <= 3)
		{
			return 270;
		}
		return 90;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
