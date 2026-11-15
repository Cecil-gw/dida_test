using System;
using System.Globalization;
using System.Windows.Data;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;

namespace ticktick_WPF.Views.Search;

public class SearchTaskTypeTextConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is SearchTypeFilter)
		{
			return (SearchTypeFilter)value switch
			{
				SearchTypeFilter.Task => Utils.GetString("Task"), 
				SearchTypeFilter.Tag => Utils.GetString("Tags"), 
				SearchTypeFilter.List => Utils.GetString("List"), 
				SearchTypeFilter.Filter => Utils.GetString("Filter"), 
				SearchTypeFilter.CalendarEvent => Utils.GetString("Event", Utils.GetString("Calendar")), 
				_ => "Category", 
			};
		}
		if (value is TaskType)
		{
			switch ((TaskType)value)
			{
			case TaskType.Task:
				return Utils.GetString("Task");
			case TaskType.Note:
				return Utils.GetString("Notes");
			case TaskType.TaskAndNote:
				return Utils.GetString("All");
			}
		}
		return Utils.GetString("Type", Utils.GetString("TaskType"));
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
