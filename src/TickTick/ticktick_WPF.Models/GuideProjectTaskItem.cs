using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class GuideProjectTaskItem
{
	public bool? allDay { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? startDate { get; set; }

	public string timeZone { get; set; }

	public string title { get; set; }
}
