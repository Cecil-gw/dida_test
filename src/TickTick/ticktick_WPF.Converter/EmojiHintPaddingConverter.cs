using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class EmojiHintPaddingConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Thickness thickness)
		{
			return new Thickness(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom - 4.0);
		}
		return new Thickness(0.0, 0.0, 0.0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
