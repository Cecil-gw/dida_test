using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class NormalCountdownForeConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		string[] array = values[0] as string[];
		string text = values[1] as string;
		if (array != null && array.Length != 0)
		{
			if (text == "normal")
			{
				bool flag = ThemeUtil.IsDark();
				if (values[2] is SolidColorBrush solidColorBrush)
				{
					flag = solidColorBrush.Color != Colors.White;
				}
				if (flag)
				{
					string text2 = array[0];
					if (text2 == "#303E75")
					{
						return ThemeUtil.GetPrimaryColor(1.0);
					}
					if (text2 == "#191919")
					{
						return new SolidColorBrush(Colors.White);
					}
				}
			}
			return ThemeUtil.GetColorInString(array[0]);
		}
		switch (text)
		{
		case "calendar":
		case "normal":
			return ThemeUtil.GetPrimaryColor(1.0);
		case "polaroid":
		case "fullscreen_image":
			return new SolidColorBrush(Colors.White);
		case "two_color_card":
			return ThemeUtil.GetColorInString("#546382");
		case "dopamine":
			return ThemeUtil.GetColorInString("#92ACFF");
		case "cartoon":
			return ThemeUtil.GetColorInString("#5F85FF");
		default:
			return ThemeUtil.GetPrimaryColor(1.0);
		}
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
