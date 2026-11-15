using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class TaskModeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string text)
		{
			if (text == "TEXT")
			{
				return Utils.GetImageSource("CheckitemsDrawingImage");
			}
			return Utils.GetImageSource("ContentDrawingImage");
		}
		return Utils.GetImageSource("ContentDrawingImage");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
