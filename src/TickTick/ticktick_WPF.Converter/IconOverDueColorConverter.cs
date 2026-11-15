using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class IconOverDueColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		string key = "BaseColorOpacity60";
		if (values != null && values.Length >= 2)
		{
			if (values[0] is DateTime dateTime)
			{
				key = ((dateTime.Date >= DateTime.Today) ? "DateColorPrimary" : "OutDateColor");
			}
			object obj = values[1];
			if (!(obj is FrameworkElement context))
			{
				if (obj is ResourceDictionary dict)
				{
					return ThemeUtil.GetColor(dict, key);
				}
				return ThemeUtil.GetColor(key);
			}
			return ThemeUtil.GetColor(key, context);
		}
		return ThemeUtil.GetColor(key);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
