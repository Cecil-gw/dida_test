using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class DayNameForegroundConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length == 2 && values[0] is string text)
		{
			if (HolidayManager.IsHolidayText(text))
			{
				if (Utils.IsJp())
				{
					return ThemeUtil.GetColorInString("#FF0000");
				}
				return ThemeUtil.GetColorInString("#5DCA94");
			}
			if (values[1] is SolidColorBrush result)
			{
				return result;
			}
		}
		return ThemeUtil.GetColor("BaseColorOpacity40");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
