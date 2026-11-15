using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ticktick_WPF.Converter;

public class StringToBrushConverter : IValueConverter
{
	private static BrushConverter converter { get; } = new BrushConverter();

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return Brushes.Transparent;
			}
			try
			{
				return converter.ConvertFromString(text);
			}
			catch
			{
				return Brushes.Transparent;
			}
		}
		return Brushes.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
