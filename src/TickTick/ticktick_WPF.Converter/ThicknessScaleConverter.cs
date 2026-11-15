using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class ThicknessScaleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double num && parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return new Thickness(0.0, result * num, 0.0, 0.0);
		}
		return new Thickness(0.0, 0.0, 0.0, 0.0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
