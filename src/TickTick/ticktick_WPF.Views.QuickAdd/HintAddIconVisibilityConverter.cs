using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.QuickAdd;

public class HintAddIconVisibilityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		bool flag = default(bool);
		int num;
		if (values.Length != 0)
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
		bool flag2 = (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		bool flag3 = default(bool);
		int num2;
		if (values.Length > 1)
		{
			object obj = values[1];
			if (obj is bool)
			{
				flag3 = (bool)obj;
				num2 = 1;
			}
			else
			{
				num2 = 0;
			}
		}
		else
		{
			num2 = 0;
		}
		bool flag4 = (byte)((uint)num2 & (flag3 ? 1u : 0u)) != 0;
		bool flag5 = default(bool);
		int num3;
		if (values.Length > 2)
		{
			object obj = values[2];
			if (obj is bool)
			{
				flag5 = (bool)obj;
				num3 = 1;
			}
			else
			{
				num3 = 0;
			}
		}
		else
		{
			num3 = 0;
		}
		if (((uint)num3 & (flag5 ? 1u : 0u)) != 0)
		{
			return Visibility.Collapsed;
		}
		return (flag2 && flag4) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
