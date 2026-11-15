using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.Misc;

public class StopPointConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2)
		{
			double num = (values[0] as double?).GetValueOrDefault();
			double valueOrDefault = (values[1] as double?).GetValueOrDefault();
			if (num >= 100.0)
			{
				num = 99.9;
			}
			double num2 = 20.0 - valueOrDefault;
			double num3 = Math.PI * 2.0 * num / 100.0;
			double x = 20.0 + Math.Sin(num3) * num2;
			double y = 20.0 - Math.Cos(num3) * num2;
			return new Point(x, y);
		}
		return new Point(20.0, 0.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
