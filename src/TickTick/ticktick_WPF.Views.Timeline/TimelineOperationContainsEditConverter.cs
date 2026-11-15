using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Timeline;

public class TimelineOperationContainsEditConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is TimelineCellOperation myEnum)
		{
			return myEnum.Contain(TimelineCellOperation.Edit);
		}
		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
