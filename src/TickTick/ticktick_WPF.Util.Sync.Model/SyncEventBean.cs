using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncEventBean
{
	[JsonProperty(PropertyName = "calendarAccountBeans")]
	public List<CalendarAccountBean> CalendarAccountBeans { get; set; } = new List<CalendarAccountBean>();

	[JsonIgnore]
	public string AccountKind { get; set; }
}
