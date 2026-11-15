using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class CalendarEventData
{
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime begin { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime end { get; set; }

	public List<string> errorIds { get; set; }

	public List<BindCalendarModel> events { get; set; }
}
