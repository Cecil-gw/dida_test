using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TabSelectColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int num && ((parameter is string s && int.TryParse(s, out var result) && num == result) || (parameter is int num2 && num == num2)))
		{
			return ThemeUtil.GetColor("PrimaryColor");
		}
		return Brushes.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
