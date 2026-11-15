using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class FilterColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			switch ((CardType)value)
			{
			case CardType.LogicAnd:
				return ThemeUtil.GetColor("LogicAndColor").ToString();
			case CardType.LogicOr:
				return ThemeUtil.GetColor("LogicOrColor").ToString();
			case CardType.Filter:
				return ThemeUtil.GetColor("BaseColorOpacity20").ToString();
			}
		}
		return ThemeUtil.GetColor("BaseColorOpacity20").ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
