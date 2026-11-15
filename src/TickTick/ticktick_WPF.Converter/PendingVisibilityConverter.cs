using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class PendingVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!(bool)value)
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
