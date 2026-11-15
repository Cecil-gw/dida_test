using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class CommentLeftCountColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			int result = 1024;
			if (parameter != null)
			{
				int.TryParse(parameter.ToString(), out result);
			}
			int.TryParse(value.ToString(), out var result2);
			if (result2 > result)
			{
				return ThemeUtil.GetColor("TextRedColor").ToString();
			}
			return ThemeUtil.GetColor("BaseColorOpacity60").ToString();
		}
		return ThemeUtil.GetColor("BaseColorOpacity100").ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
