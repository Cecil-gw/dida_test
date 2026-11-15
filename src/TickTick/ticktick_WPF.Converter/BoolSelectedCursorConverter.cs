using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ticktick_WPF.Converter;

public class BoolSelectedCursorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool)
		{
			if (!(bool)value)
			{
				return Cursors.Arrow;
			}
			return Cursors.Hand;
		}
		return Cursors.Arrow;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
