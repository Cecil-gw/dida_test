using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class WidgetTaskTitleColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 3)
		{
			int? num = values[0] as int?;
			string value = values[1] as string;
			if (values[2] is ResourceDictionary dict)
			{
				if ((num.HasValue && num != 0) || string.IsNullOrEmpty(value))
				{
					return ThemeUtil.GetColor(dict, "BaseColorOpacity40");
				}
				return ThemeUtil.GetColor(dict, "BaseColorOpacity100_80");
			}
		}
		return new SolidColorBrush(Colors.White);
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
