using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class BackgroundColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			switch ((CardType)value)
			{
			case CardType.LogicAnd:
				return ThemeUtil.GetColor("LogicAndBackground").ToString();
			case CardType.LogicOr:
				return ThemeUtil.GetColor("LogicOrBackground").ToString();
			case CardType.InitFilter:
				return new SolidColorBrush(Colors.Transparent);
			case CardType.InitLogic:
				return ThemeUtil.GetColor("BaseColorOpacity5").ToString();
			}
		}
		return new SolidColorBrush(Colors.Transparent);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
