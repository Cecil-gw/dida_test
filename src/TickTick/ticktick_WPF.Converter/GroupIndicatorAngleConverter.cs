using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class GroupIndicatorAngleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		int num = 0;
		int num2 = 90;
		if (parameter is string text)
		{
			string[] array = text.Split(';');
			if (array.Length == 2 && int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2))
			{
				num = result;
				num2 = result2;
			}
		}
		if (value == null || !(bool)value)
		{
			return num2;
		}
		return num;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
