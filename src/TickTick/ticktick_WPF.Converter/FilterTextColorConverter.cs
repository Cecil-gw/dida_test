using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class FilterTextColorConverter : IValueConverter
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
			case CardType.InitFilter:
				return ThemeUtil.GetColor("BaseColorOpacity20").ToString();
			case CardType.InitLogic:
				return ThemeUtil.GetColor("BaseColorOpacity40").ToString();
			case CardType.AddMore:
				return ThemeUtil.GetColor("PrimaryColor").ToString();
			case CardType.Filter:
				return ThemeUtil.GetColor("BaseColorOpacity100_80").ToString();
			}
		}
		return new SolidColorBrush(Colors.Transparent);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
