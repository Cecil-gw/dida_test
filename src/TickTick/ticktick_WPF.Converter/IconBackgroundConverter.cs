using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class IconBackgroundConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2)
		{
			if (values[0] != null && values[0] is SolidColorBrush { Color: { A: not 0 } } solidColorBrush)
			{
				return solidColorBrush;
			}
			if (values[1] is string)
			{
				return ThemeUtil.GetPrimaryColor(0.1);
			}
		}
		return new SolidColorBrush();
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
