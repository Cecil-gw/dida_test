using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class KanbanItemShadowConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2 && values[0] is bool flag && values[1] is bool flag2)
		{
			if (!flag && !flag2)
			{
				return new Thickness(6.0, 0.0, 6.0, 0.0);
			}
			if (flag && !flag2)
			{
				return new Thickness(6.0, 6.0, 6.0, 0.0);
			}
			if (!flag)
			{
				return new Thickness(6.0, 0.0, 6.0, 6.0);
			}
			return new Thickness(6.0);
		}
		return new Thickness(6.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
