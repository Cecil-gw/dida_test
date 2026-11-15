using System.Collections.Generic;
using System.Linq;

namespace TickTickModels;

public class CalendarContentProject
{
	public string id { get; set; }

	public string calendarId { get; set; }

	public string syncProjectId { get; set; }

	public string syncProjectScope { get; set; }

	public List<string> defaultTags { get; set; }

	public CalendarContentProject Copy()
	{
		return (CalendarContentProject)MemberwiseClone();
	}

	public bool IsEquals(CalendarContentProject modelProject)
	{
		if (!(syncProjectId != modelProject.syncProjectId) && defaultTags?.Count == modelProject.defaultTags?.Count)
		{
			if (modelProject.defaultTags != null)
			{
				return defaultTags?.Union(modelProject.defaultTags).Count() != defaultTags?.Count;
			}
			return false;
		}
		return true;
	}
}
