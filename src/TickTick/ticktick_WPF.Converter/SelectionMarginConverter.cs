using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Converter;

public class SelectionMarginConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Length == 2)
		{
			SelectionMode valueOrDefault = (values[0] as SelectionMode?).GetValueOrDefault();
			double valueOrDefault2 = (values[1] as double?).GetValueOrDefault();
			if (valueOrDefault == SelectionMode.End || valueOrDefault == SelectionMode.Start)
			{
				double num = (valueOrDefault2 - 30.0) / 2.0;
				return new Thickness((valueOrDefault == SelectionMode.Start) ? num : 0.0, 0.0, (valueOrDefault == SelectionMode.End) ? num : 0.0, 0.0);
			}
			return new Thickness(0.0);
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
