using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CountdownForeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string[] array && array.Length != 0)
		{
			if (ThemeUtil.IsDark() && parameter is string text && text == "normal")
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
			return ThemeUtil.GetColorInString(array[0]);
		}
		if (parameter is string text3)
		{
			if (text3 == "normal" || text3 == "calendar")
			{
				return ThemeUtil.GetPrimaryColor(1.0);
			}
			if (!(text3 == "fullscreen_image"))
			{
				switch (text3)
				{
				case "fullscreen_image":
					break;
				case "two_color_card":
					return ThemeUtil.GetColorInString("#546382");
				case "dopamine":
					return ThemeUtil.GetColorInString("#92ACFF");
				case "cartoon":
					return ThemeUtil.GetColorInString("#5F85FF");
				default:
					goto IL_0112;
				}
			}
			return new SolidColorBrush(Colors.White);
		}
		goto IL_0112;
		IL_0112:
		return ThemeUtil.GetPrimaryColor(1.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
