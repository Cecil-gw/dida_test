using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class SubItemBackgroundConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 6)
		{
			object obj = values[5];
			if (obj is bool && !(bool)obj)
			{
				return Brushes.Transparent;
			}
			if (values[0] is bool flag && values[1] is bool flag2 && values[2] is bool flag3)
			{
				if (flag)
				{
					return ThemeUtil.GetColor("ItemSelectedColor");
				}
				if (flag2 || flag3)
				{
					obj = values[4];
					bool flag4 = default(bool);
					int num;
					if (obj is bool)
					{
						flag4 = (bool)obj;
						num = 1;
					}
					else
					{
						num = 0;
					}
					if (((uint)num & (flag4 ? 1u : 0u)) != 0)
					{
						return Brushes.Transparent;
					}
					return ThemeUtil.GetColor("BaseColorOpacity5");
				}
			}
		}
		return Brushes.Transparent;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
