using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ticktick_WPF.Converter;

public class EditableCursorConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null && value is bool && !(bool)value)
		{
			return Cursors.No;
		}
		return Cursors.IBeam;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
