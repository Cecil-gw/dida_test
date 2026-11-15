using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class LoadMoreWidthConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double num)
		{
			return num - 40.0;
		}
		return 0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
