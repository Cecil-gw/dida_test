using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class DotLineColorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			switch ((CardType)value)
			{
			case CardType.InitFilter:
				return ThemeUtil.GetColor("BaseColorOpacity10_20").ToString();
			case CardType.InitLogic:
				return ThemeUtil.GetColor("BaseColorOpacity40").ToString();
			}
		}
		return new SolidColorBrush(Colors.White);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
