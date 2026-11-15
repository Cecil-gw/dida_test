using System.Collections.Generic;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class CalendarEventBean
{
	[JsonProperty(PropertyName = "calendarId")]
	public string CalendarId { get; set; }

	[JsonProperty(PropertyName = "add")]
	public List<CalendarEventModel> Add { get; set; } = new List<CalendarEventModel>();

	[JsonProperty(PropertyName = "update")]
	public List<CalendarEventModel> Update { get; set; } = new List<CalendarEventModel>();

	[JsonProperty(PropertyName = "delete")]
	public List<string> Delete { get; set; } = new List<string>();

	[JsonProperty(PropertyName = "move")]
	public List<EventMoveModel> Move { get; set; } = new List<EventMoveModel>();
}
