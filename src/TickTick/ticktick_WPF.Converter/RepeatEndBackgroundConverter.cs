using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class RepeatEndBackgroundConverter : IValueConverter
{
	public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values is string text && (text.ToUpper().Contains("UNTIL") || text.ToUpper().Contains("COUNT")))
		{
			return ThemeUtil.GetColor("BaseColorOpacity5").ToString();
		}
		return ThemeUtil.GetColor("BaseColorOpacity0_10").ToString();
	}

	public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
