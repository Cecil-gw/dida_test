using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync.Model;

public class CalendarAccountBean
{
	[JsonProperty(PropertyName = "accountId")]
	public string AccountId { get; set; }

	[JsonProperty(PropertyName = "calendarEventBeans")]
	public List<CalendarEventBean> CalendarBeans { get; set; }
}
