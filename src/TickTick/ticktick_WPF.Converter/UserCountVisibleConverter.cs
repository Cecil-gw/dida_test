using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class UserCountVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int num)
		{
			if (num <= 1)
			{
				return "Collapsed";
			}
			return "Visible";
		}
		return "Collapsed";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
