using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class FilterTypeColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && parameter != null)
		{
			switch ((FilterMode)value)
			{
			case FilterMode.Normal:
				if (!(parameter.ToString() == "normal"))
				{
					return ThemeUtil.GetColor("BaseColorOpacity60").ToString();
				}
				return ThemeUtil.GetColor("TextAccentColor").ToString();
			case FilterMode.Advanced:
				if (!(parameter.ToString() == "normal"))
				{
					return ThemeUtil.GetColor("TextAccentColor").ToString();
				}
				return ThemeUtil.GetColor("BaseColorOpacity60").ToString();
			}
		}
		return ThemeUtil.GetColor("black_04").ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
