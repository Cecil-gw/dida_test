using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class HighlightTextConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		bool flag = default(bool);
		int num;
		if (values.Length == 2)
		{
			object obj = values[0];
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) == 0)
		{
			return ThemeUtil.GetColor("BaseColorOpacity100_80");
		}
		return ThemeUtil.GetColor("TextAccentColor");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
