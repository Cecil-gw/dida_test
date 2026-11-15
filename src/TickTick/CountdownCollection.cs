using Newtonsoft.Json;
using ticktick_WPF.Models;

public class CountdownCollection
{
	[JsonProperty(PropertyName = "countdowns")]
	public CountdownModel[] Countdowns { get; set; }
}
