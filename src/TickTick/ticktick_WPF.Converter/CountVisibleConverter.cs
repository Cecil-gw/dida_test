using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class CountVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			int.TryParse(value.ToString(), out var result);
			if (result <= 0)
			{
				return Visibility.Collapsed.ToString();
			}
			return Visibility.Visible.ToString();
		}
		return Visibility.Collapsed.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
