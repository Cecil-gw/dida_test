using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class HabitSettingsModel : ErrorModel
{
	[JsonProperty("showInCalendar")]
	public bool? ShowInCalendar { get; set; }

	[JsonProperty("showInToday")]
	public bool? ShowInToday { get; set; }

	[JsonProperty("enabled")]
	public bool? Enabled { get; set; }

	[JsonProperty("defaultSection")]
	public HabitDefaultSection DefaultSection { get; set; }
}
