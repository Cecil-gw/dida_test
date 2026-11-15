using System;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

[Serializable]
public class HabitDefaultSection
{
	[JsonProperty("order")]
	public long SortOrder { get; set; }
}
