using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class BindCalendarsCollection : BaseModel
{
	[JsonProperty("events")]
	public List<BindCalendarModel> Events { get; set; }

	[JsonProperty("errorIds")]
	public List<string> ErrorIds { get; set; }
}
