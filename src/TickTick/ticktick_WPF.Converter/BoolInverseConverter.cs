using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class BoolInverseConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is bool flag)
		{
			return !flag;
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
