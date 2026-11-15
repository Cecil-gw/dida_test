using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class CalendarAttendeeModel : BaseModel
{
	[JsonProperty("email")]
	public string Email { get; set; }

	[JsonProperty("responseStatus")]
	public string ResponseStatus { get; set; }

	[JsonProperty("self")]
	public bool Self { get; set; }

	[JsonProperty("organizer")]
	public bool Organizer { get; set; }

	[JsonIgnore]
	public bool FirstGuest { get; set; }

	[JsonIgnore]
	public string EventId { get; set; }
}
