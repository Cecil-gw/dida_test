using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class LineX2Converter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			double num = (double)value;
			if (num > 20.0)
			{
				return num - 10.0;
			}
		}
		return 10;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
