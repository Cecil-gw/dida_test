using System;
using System.Globalization;
using System.Windows.Data;
using TickTickUtils;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class PomoSoundPathConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string key)
		{
			return Utils.GetGeometry("Ic.Focus." + FocusConstance.GetSoundIndex(key));
		}
		return Utils.GetGeometry("Ic.Focus.Mute");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
