using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineIconFillConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && !((values[0] as bool?) ?? true) && values[1] as bool? == true)
		{
			return ThemeUtil.GetColor("PrimaryColor");
		}
		return ThemeUtil.GetColor("BaseColorOpacity40");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
