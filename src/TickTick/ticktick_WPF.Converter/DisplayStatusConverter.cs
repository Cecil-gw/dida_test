using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Converter;

public class DisplayStatusConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int num)
		{
			return num switch
			{
				0 => Utils.GetString("smart_list_on_the_day_title"), 
				-3 => string.Format(Utils.GetString("smart_list_xx_days_advance_title"), 3), 
				-7 => string.Format(Utils.GetString("smart_list_xx_days_advance_title"), 7), 
				-9999 => Utils.GetString("smart_list_always_show_title"), 
				9999 => Utils.GetString("smart_list_not_show_title"), 
				_ => "", 
			};
		}
		return "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return 0;
	}
}
