using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class OpacityToolTipConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is float num)
		{
			return (int)((1f - num) * 100f) + "%";
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
