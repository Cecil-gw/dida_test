using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class PomoDurationTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is int num)
		{
			return ((float)num * 1f / 60f).ToString("0.##") + "h";
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
