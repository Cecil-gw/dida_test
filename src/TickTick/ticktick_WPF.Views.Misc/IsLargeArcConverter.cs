using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Misc;

public class IsLargeArcConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double num)
		{
			return num > 50.0;
		}
		return true;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
