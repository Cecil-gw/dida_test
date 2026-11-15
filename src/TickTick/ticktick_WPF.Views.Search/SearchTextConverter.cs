using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Search;

public class SearchTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!(value is string text))
		{
			return "";
		}
		return "\"" + text + "\"";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
