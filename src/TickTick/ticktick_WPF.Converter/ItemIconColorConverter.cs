using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class ItemIconColorConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 5 && values[0] is DisplayType displayType && values[1] is int num && values[2] is int num2)
		{
			FrameworkElement context = ((values.Length == 5) ? (values[4] as FrameworkElement) : null);
			if (num2 == 0 && (displayType == DisplayType.Task || displayType == DisplayType.CheckItem || displayType == DisplayType.Agenda))
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
			if (num2 != 0)
			{
				return ThemeUtil.GetColor((values[3] as bool? == true) ? "BaseColorOpacity40" : "BaseColorOpacity20", context);
			}
			return ThemeUtil.GetColor("BaseColorOpacity40", context);
		}
		return ThemeUtil.GetColor("BaseColorOpacity40");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
