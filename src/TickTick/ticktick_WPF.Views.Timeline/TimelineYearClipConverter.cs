using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ticktick_WPF.Views.Timeline;

public class TimelineYearClipConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 4 && values[0] is double num && values[1] is double num2 && values[2] is double num3 && values[3] is double height)
		{
			double num4 = num - num3 - num2;
			if (num4 > 0.0)
			{
				return new RectangleGeometry(new Rect(num2, 0.0, num4, height));
			}
		}
		return null;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
