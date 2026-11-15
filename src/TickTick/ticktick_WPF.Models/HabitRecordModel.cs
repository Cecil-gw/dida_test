using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class HabitRecordModel : BaseModel
{
	[JsonProperty(PropertyName = "content")]
	public string Content { get; set; }

	[JsonProperty(PropertyName = "habitId")]
	public string HabitId { get; set; }

	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "stamp")]
	public int Stamp { get; set; }

	[JsonProperty(PropertyName = "emoji")]
	public int Emoji { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? opTime { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonIgnore]
	public int Status { get; set; }

	[JsonIgnore]
	public int Deleted { get; set; }
}
