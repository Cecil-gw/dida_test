using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class HabitCheckInModel : BaseModel
{
	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; }

	[JsonProperty(PropertyName = "habitId")]
	[Indexed]
	public string HabitId { get; set; }

	[JsonProperty(PropertyName = "checkinStamp")]
	public string CheckinStamp { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	[JsonProperty(PropertyName = "checkinTime")]
	public DateTime? CheckinTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? opTime { get; set; }

	[JsonProperty(PropertyName = "value")]
	public double Value { get; set; }

	[JsonProperty(PropertyName = "goal")]
	public double Goal { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonIgnore]
	[Indexed]
	public int Status { get; set; }

	[JsonProperty(PropertyName = "status")]
	public int CheckStatus { get; set; }

	public double GetDisplayValue()
	{
		return Math.Max(0.0, Value);
	}

	public bool IsUnComplete()
	{
		return CheckStatus == 1;
	}

	public bool IsComplete()
	{
		if (Value >= Goal)
		{
			return CheckStatus != 1;
		}
		return false;
	}
}
