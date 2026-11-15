using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CalendarTaskCellColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3 && values[0] is int num)
		{
			string color = values[1] as string;
			if (ThemeUtil.IsEmptyColor(color))
			{
				color = ThemeUtil.GetColor("PrimaryColor").ToString().Replace("#FF", "#");
			}
			if (num != 0)
			{
				return ThemeUtil.GetAlphaColor(color, 36);
			}
			return ThemeUtil.GetColorInString(color);
		}
		return new SolidColorBrush(Colors.Transparent);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
