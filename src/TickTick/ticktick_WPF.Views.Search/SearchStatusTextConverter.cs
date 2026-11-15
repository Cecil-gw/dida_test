using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Search;

public class SearchStatusTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value != null)
		{
			switch ((StatusFilter)value)
			{
			case StatusFilter.All:
				return Utils.GetString("AllStatus");
			case StatusFilter.Uncompleted:
				return Utils.GetString("Uncompleted");
			case StatusFilter.Completed:
				return Utils.GetString("Completed");
			case StatusFilter.Abandoned:
				return Utils.GetString("Abandoned");
			}
		}
		return Utils.GetString("AllStatus");
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
