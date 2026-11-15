using System.Collections.Generic;

namespace TickTickModels;

public class ProjectBindCalendarModel
{
	public string calendarId { get; set; }

	public string syncProjectId { get; set; }

	public string syncProjectScope { get; set; }

	public List<string> defaultTags { get; set; }
}
