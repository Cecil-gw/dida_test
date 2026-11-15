using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ComoBoxMinWidth : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return (double.Parse(value.ToString()) - 2.0 > 0.0) ? (double.Parse(value.ToString()) - 2.0) : 0.0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
