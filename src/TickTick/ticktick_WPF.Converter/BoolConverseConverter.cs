using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class BoolConverseConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			return !(bool)value;
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
