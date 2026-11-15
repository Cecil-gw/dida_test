using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class StyleMatchVisibilityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values.Length < 2 || values[0] == null || values[1] == null)
		{
			return Visibility.Collapsed;
		}
		return (!(values[0].ToString() == values[1].ToString()) && (!(values[1].ToString() == "polaroid") || !(values[0].ToString() == "fullscreen_image")) && (!(values[1].ToString() == "calendar") || !(values[0].ToString() == "normal"))) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
