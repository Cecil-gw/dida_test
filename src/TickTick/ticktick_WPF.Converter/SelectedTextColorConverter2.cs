using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class SelectedTextColorConverter2 : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value == null || !(bool)value)
		{
			return ThemeUtil.GetColor("BaseColorOpacity40_20").ToString();
		}
		return ThemeUtil.GetColor("TextAccentColor").ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
