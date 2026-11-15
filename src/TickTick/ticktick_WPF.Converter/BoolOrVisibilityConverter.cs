using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class BoolOrVisibilityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		bool flag = false;
		List<int> list = null;
		if (parameter != null)
		{
			list = parameter.ToString().Split(',').Select(int.Parse)
				.ToList();
		}
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] is bool flag2 && ((flag2 && (list == null || !list.Contains(i))) || (!flag2 && list != null && list.Contains(i))))
			{
				flag = true;
				break;
			}
		}
		return (!flag) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
