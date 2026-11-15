using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class SelectedOrDisabledTextColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		string key = "BaseColorOpacity100_80";
		if (parameter is string text)
		{
			key = text;
		}
		FrameworkElement context = values[2] as FrameworkElement;
		if (values[0] != null && !(bool)values[0])
		{
			return ThemeUtil.GetColor("BaseColorOpacity40", context);
		}
		if (values[1] == null || !(bool)values[1])
		{
			return ThemeUtil.GetColor(key, context);
		}
		return ThemeUtil.GetColor("TextAccentColor", context);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
