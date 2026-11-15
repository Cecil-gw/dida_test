using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineInlineFrontWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 5 && values[0] is double num && values[1] is double num2 && values[2] is double num3 && values[3] is double num4)
		{
			object obj = values[4];
			if (obj is bool)
			{
				double num5 = (((bool)obj) ? num4 : 0.0);
				if (num2 < num + num5 && num3 > 104.0)
				{
					return Math.Min(num + num5 - num2 + 8.0, num3 - 104.0);
				}
			}
		}
		return 8;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
