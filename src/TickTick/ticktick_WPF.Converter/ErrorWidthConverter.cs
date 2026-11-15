using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ErrorWidthConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double num)
		{
			return num / 2.0 - 85.0;
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
