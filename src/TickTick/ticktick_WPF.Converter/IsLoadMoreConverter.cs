using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class IsLoadMoreConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && (bool)value)
		{
			return "Visible";
		}
		return "Collapsed";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
