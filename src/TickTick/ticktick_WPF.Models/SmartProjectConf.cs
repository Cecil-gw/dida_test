using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class SmartProjectConf
{
	public static readonly List<string> ProjectSectionName = new List<string> { "project", "tag", "filter", "subscribedCalendar" };

	public string name;

	public string visibility = "show";

	public long? order;
}
