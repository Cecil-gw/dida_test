using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class DragBackgroundConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			string text = value.ToString();
			if (!ThemeUtil.IsEmptyColor(text))
			{
				return text;
			}
		}
		return ThemeUtil.GetColor("PrimaryColor");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
