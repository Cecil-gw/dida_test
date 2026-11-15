using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class ProgressTextColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2)
		{
			object obj = values[0];
			if (obj is int && (int)obj != 0)
			{
				return ThemeUtil.GetColor("BaseColorOpacity40");
			}
		}
		return ThemeUtil.GetColor("BaseColorOpacity60");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
