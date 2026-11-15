using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class DateIndexConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		int result = 7;
		if (value == null)
		{
			return result;
		}
		int.TryParse(value.ToString(), out result);
		if (result < 30)
		{
			return result - 1;
		}
		return 7;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
