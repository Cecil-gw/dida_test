using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineTextColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 3)
		{
			if (!((values[0] as bool?) ?? true) && values[1] as bool? == true)
			{
				return ThemeUtil.GetColor("PrimaryColor");
			}
			return values[2] as SolidColorBrush;
		}
		return Brushes.White;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
