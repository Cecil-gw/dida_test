using System;
using Newtonsoft.Json;

namespace TickTickModels;

public class ReminderDelayModel : BaseModel
{
	[JsonProperty("objId")]
	public string ObjId { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; }

	public string op { get; set; }

	[JsonProperty("remindTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? RemindTime { get; set; }

	[JsonProperty("nextTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? NextTime { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonIgnore]
	public int SyncStatus { get; set; }
}
