using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class CalendarTaskDateVisibilityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3)
		{
			bool flag = default(bool);
			int num;
			if (values[0] != null)
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
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				return Visibility.Visible;
			}
			if (values[1] is DateTime dateTime && values[2] is bool flag2 && dateTime < DateTime.Today && flag2)
			{
				return Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
