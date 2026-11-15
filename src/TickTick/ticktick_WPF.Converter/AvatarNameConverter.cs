using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class AvatarNameConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values != null && values.Any() && values[0] is string text && values[1] is string text2)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2) || text == "-1")
			{
				return Utils.GetString("AssignTo");
			}
			return AvatarHelper.GetCacheUserName(text, text2);
		}
		return Utils.GetString("AssignTo");
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		return null;
	}
}
