using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync.Model;

public class EventMoveModel
{
	[JsonProperty(PropertyName = "destination")]
	public string Destination { get; set; }

	[JsonProperty(PropertyName = "eventId")]
	public string EventId { get; set; }
}
