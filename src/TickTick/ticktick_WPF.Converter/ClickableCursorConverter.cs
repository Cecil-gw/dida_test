using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ticktick_WPF.Converter;

public class ClickableCursorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is bool && !(bool)value)
		{
			return Cursors.Arrow;
		}
		return Cursors.Hand;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
