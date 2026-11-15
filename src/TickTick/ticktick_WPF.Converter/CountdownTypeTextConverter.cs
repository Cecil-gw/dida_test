using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Models;

namespace ticktick_WPF.Converter;

public class CountdownTypeTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is CountDownType type)
		{
			return CountdownModel.GetTypeText(type);
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
