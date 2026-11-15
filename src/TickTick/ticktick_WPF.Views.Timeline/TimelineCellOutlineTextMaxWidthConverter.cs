using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineCellOutlineTextMaxWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		double val = 120.0;
		if (values.Length == 3 && values[0] is double num && values[1] is double num2)
		{
			object obj = values[2];
			if (obj is bool)
			{
				int num3 = (((bool)obj) ? 20 : 0);
				val = Math.Min(120.0, num - num2 - 9.0 - (double)num3);
			}
		}
		return Math.Max(30.0, val);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
