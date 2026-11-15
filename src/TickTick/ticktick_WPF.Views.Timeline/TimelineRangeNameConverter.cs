using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Timeline;

public class TimelineRangeNameConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string key)
		{
			return Utils.GetString(TimelineConstants.GetRangeI18nKey(key));
		}
		return Utils.GetString(TimelineConstants.GetRangeI18nKey(TimelineConstants.RangeDefault));
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
