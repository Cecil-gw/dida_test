using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ticktick_WPF.Views.Search;

public class SearchTaskTypeIconConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		string resourceKey = "Ic.Search.Completed";
		if (value is SearchTypeFilter)
		{
			resourceKey = (SearchTypeFilter)value switch
			{
				SearchTypeFilter.Tag => "Ic.Search.Tag", 
				SearchTypeFilter.List => "Ic.Search.List", 
				SearchTypeFilter.Filter => "Ic.Search.Filter", 
				SearchTypeFilter.CalendarEvent => "Ic.Search.CalendarSubscribe", 
				_ => "Ic.Search.Completed", 
			};
		}
		return Application.Current?.TryFindResource(resourceKey);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
