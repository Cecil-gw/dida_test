using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Converter;

public class AttachmentSizedConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if ((int)value < 1024)
		{
			return value?.ToString() + "B";
		}
		if ((int)value < 1024000)
		{
			return ((double)(int)value / 1024.0).ToString("f2") + "KB";
		}
		return ((double)(int)value / 1024000.0).ToString("f2") + "MB";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
