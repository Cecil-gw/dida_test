using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class SectionOpenConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool)
		{
			if ((bool)value)
			{
				return 0;
			}
			if (parameter is string s && int.TryParse(s, out var result))
			{
				return result;
			}
			return 270;
		}
		return 0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
