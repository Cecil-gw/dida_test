using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class BoolOpacityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is bool)
		{
			return ((bool)value) ? 0f : 1f;
		}
		return 1f;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
