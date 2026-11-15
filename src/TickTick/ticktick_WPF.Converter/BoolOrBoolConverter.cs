using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class BoolOrBoolConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		List<int> list = null;
		if (parameter != null)
		{
			list = parameter.ToString().Split(',').Select(int.Parse)
				.ToList();
		}
		if (values.Length != 0)
		{
			for (int i = 0; i < values.Length; i++)
			{
				if (values[i] is bool flag && ((flag && (list == null || !list.Contains(i))) || (!flag && list != null && list.Contains(i))))
				{
					return true;
				}
			}
		}
		return false;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
