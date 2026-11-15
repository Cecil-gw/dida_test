using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class AttachmentShowKindConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value.ToString() == "IMAGE")
		{
			if (parameter.ToString() == "image")
			{
				return "Visible";
			}
			return "Collapsed";
		}
		if (parameter.ToString() == "image")
		{
			return "Collapsed";
		}
		return "Visible";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
