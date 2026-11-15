using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class ItemCheckIconBackgroundConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 5 && values[0] is DisplayType displayType && values[1] is int num && values[2] is int num2)
		{
			object obj = values[3];
			if (obj is bool)
			{
				if (!(bool)obj || displayType == DisplayType.Note || displayType == DisplayType.Event || num2 != 0)
				{
					return Brushes.Transparent;
				}
				ResourceDictionary dict = ((values.Length == 5) ? (values[4] as ResourceDictionary) : null);
				if (displayType == DisplayType.Task || displayType == DisplayType.CheckItem || displayType == DisplayType.Agenda)
				{
					switch (num)
					{
					case 1:
						return ThemeUtil.GetColor("PriorityLowColor");
					case 3:
						return ThemeUtil.GetColor("PriorityMiddleColor");
					case 5:
						return ThemeUtil.GetColor("PriorityHighColor");
					}
				}
				return ThemeUtil.GetColor("BaseColorOpacity100", dict);
			}
		}
		return Brushes.Transparent;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
