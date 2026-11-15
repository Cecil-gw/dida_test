using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Converter;

public class DotBorderVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			CardType cardType = (CardType)value;
			if (cardType != CardType.InitFilter && cardType != CardType.InitLogic)
			{
				return Visibility.Collapsed;
			}
		}
		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
