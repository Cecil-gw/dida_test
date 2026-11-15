using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class RepeatBackgroundColorConverter : IValueConverter
{
	public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
	{
		if (string.IsNullOrEmpty(values?.ToString()) || values.ToString().Contains("NONE"))
		{
			return ThemeUtil.GetColor("BaseColorOpacity0_10").ToString();
		}
		return ThemeUtil.GetColor("BaseColorOpacity5").ToString();
	}

	public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
