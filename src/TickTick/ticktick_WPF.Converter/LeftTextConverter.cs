using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class LeftTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is string { Length: >1024 } text)
		{
			return (1024 - text.Length).ToString();
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
