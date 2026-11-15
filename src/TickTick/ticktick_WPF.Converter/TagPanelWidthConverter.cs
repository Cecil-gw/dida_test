using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TagPanelWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3 && values[0] is double num && values[1] is bool flag && values[2] is bool flag2)
		{
			if (flag)
			{
				num -= 150.0;
			}
			if (flag2)
			{
				double num2 = num - 100.0;
			}
		}
		return 300;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
