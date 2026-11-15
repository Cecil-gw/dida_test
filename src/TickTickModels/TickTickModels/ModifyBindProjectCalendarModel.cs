using System.Collections.Generic;

namespace TickTickModels;

public class ModifyBindProjectCalendarModel
{
	public string id { get; set; }

	public string syncProjectId { get; set; }

	public List<string> defaultTags { get; set; }
}
