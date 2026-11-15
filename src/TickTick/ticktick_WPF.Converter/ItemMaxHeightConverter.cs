using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ItemMaxHeightConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool)
		{
			return ((bool)value) ? 40 : 120;
		}
		return 120;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
