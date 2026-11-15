using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class SubscribeCalendarModel
{
	public string id { get; set; }

	public string name { get; set; }

	public string color { get; set; }

	public List<CalendarEventModel> events { get; set; }
}
