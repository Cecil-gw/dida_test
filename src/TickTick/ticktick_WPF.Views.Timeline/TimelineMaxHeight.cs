using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineMaxHeight : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is double num && values[1] is double num2)
		{
			if (num2 < 60.0)
			{
				return 16.0;
			}
			return num;
		}
		return double.PositiveInfinity;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
