using System;
using System.Globalization;
using System.Windows.Data;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class PomoTypeTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text)
		{
			if (!(text == FocusConstance.Timing))
			{
				return Utils.GetString("PomoFocus");
			}
			return Utils.GetString("Timing");
		}
		return Utils.GetString("PomoFocus");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
