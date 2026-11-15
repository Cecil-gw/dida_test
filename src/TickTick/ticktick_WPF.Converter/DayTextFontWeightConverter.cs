using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Converter;

public class DayTextFontWeightConverter : IValueConverter
{
	public object Convert(object values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values is ShowMode)
		{
			return ((ShowMode)values == ShowMode.Today) ? FontWeights.Bold : FontWeights.Normal;
		}
		return FontWeights.Normal;
	}

	public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
