using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class SectionOptionVisibleConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 5)
		{
			object obj = values[4];
			bool flag = default(bool);
			int num;
			if (obj is bool)
			{
				flag = (bool)obj;
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (flag ? 1u : 0u)) != 0)
			{
				return Visibility.Collapsed;
			}
			obj = values[3];
			if (obj is bool && !(bool)obj)
			{
				return Visibility.Collapsed;
			}
			obj = values[2];
			bool flag2 = default(bool);
			int num2;
			if (obj is bool)
			{
				flag2 = (bool)obj;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
			if (((uint)num2 & (flag2 ? 1u : 0u)) != 0)
			{
				return Visibility.Visible;
			}
			obj = values[0];
			if (obj is bool && (bool)obj)
			{
				obj = values[1];
				if (obj is bool && !(bool)obj)
				{
					return Visibility.Visible;
				}
			}
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
