using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class TextSubstringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text && parameter is string s)
		{
			if (int.TryParse(s, out var result))
			{
				if (text.Length <= result)
				{
					return text;
				}
				return text.Substring(0, result);
			}
			return text;
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
