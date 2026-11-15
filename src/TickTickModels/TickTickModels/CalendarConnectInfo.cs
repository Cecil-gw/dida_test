using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SQLite;

namespace TickTickModels;

public class CalendarConnectInfo : BaseModel
{
	public string id { get; set; }

	public string account { get; set; }

	[Ignore]
	public List<CalendarContentCalendar> calendars { get; set; }

	[JsonIgnore]
	public string calendarString { get; set; }

	[Ignore]
	public List<CalendarContentProject> relProjects { get; set; }

	[JsonIgnore]
	public string relProjectsString { get; set; }

	public string site { get; set; }

	public int status { get; set; }

	public string userId { get; set; }

	public string workspaceName { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? createdTime { get; set; }

	public void GetItems()
	{
		if (!string.IsNullOrEmpty(relProjectsString))
		{
			relProjects = JsonConvert.DeserializeObject<List<CalendarContentProject>>(relProjectsString);
		}
		if (!string.IsNullOrEmpty(calendarString))
		{
			calendars = JsonConvert.DeserializeObject<List<CalendarContentCalendar>>(calendarString);
		}
	}
}
