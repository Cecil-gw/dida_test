using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ThemeToSummaryUrlConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is string result)
		{
			return result;
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
