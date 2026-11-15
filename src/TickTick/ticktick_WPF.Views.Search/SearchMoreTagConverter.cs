using System;
using System.Globalization;
using System.Windows.Data;

namespace ticktick_WPF.Views.Search;

public class SearchMoreTagConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (!(value is int num))
		{
			return "";
		}
		return "+" + num;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
