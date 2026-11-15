using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineFloatingTextWidthConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 4 && values[0] is double num && values[1] is int num2 && values[2] is string text)
		{
			bool flag = text != "-1" && !string.IsNullOrEmpty(text);
			bool flag2 = num2 > 0;
			if (num < 70.0 && values[3] is string { Length: <10 } text2)
			{
				double num3 = (string.IsNullOrEmpty(text2) ? 0.0 : Utils.MeasureString(text2, new Typeface(Utils.GetDefaultFontFamily(), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 12.0).Width);
				if (num < 40.0)
				{
					flag2 = num - 16.0 - num3 >= 14.0;
				}
				flag = num - 16.0 - num3 - (double)(flag2 ? 14 : 0) >= 22.0;
			}
			return Math.Max(0.0, num - 16.0 - (double)(flag2 ? 14 : 0) - (double)(flag ? 22 : 0));
		}
		return double.PositiveInfinity;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
