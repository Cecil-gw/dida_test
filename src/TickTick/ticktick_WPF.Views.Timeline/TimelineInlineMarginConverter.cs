using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineInlineMarginConverter : IMultiValueConverter
{
	public static bool IgnoreLeftOutSide;

	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length > 6 || (values.Length == 6 && !IgnoreLeftOutSide))
		{
			if (!((values[0] as bool?) ?? true))
			{
				return new Thickness(3.0, 0.0, 0.0, 0.0);
			}
			if (values[1] is double num && values[2] is double num2 && values[3] is double num3 && values[4] is double num4)
			{
				object obj = values[5];
				if (obj is bool)
				{
					double num5 = (((bool)obj) ? num4 : 0.0);
					if (values.Length > 6)
					{
						if (num5 - num2 > 0.0)
						{
							return new Thickness(num5 - num2 - 4.0, 0.0, 0.0, 0.0);
						}
						return new Thickness(0.0);
					}
					if (num2 < num + num5 && num3 > 104.0)
					{
						return new Thickness(Math.Min(num + num5 - num2 + 4.0, num3 - 104.0), 0.0, 8.0, 0.0);
					}
				}
			}
		}
		return new Thickness(8.0, 0.0, 8.0, 0.0);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
