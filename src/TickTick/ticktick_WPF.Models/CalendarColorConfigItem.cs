using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class CalendarColorConfigItem
{
	[JsonProperty("colorType")]
	public string ColorType { get; set; }

	[JsonProperty("sharedColor")]
	public string SharedColor { get; set; }

	[JsonProperty("colorMap")]
	public Dictionary<string, string> ColorMap { get; set; }
}
