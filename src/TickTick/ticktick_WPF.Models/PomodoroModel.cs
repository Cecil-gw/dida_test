using System;
using System.ComponentModel;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Models;

[Serializable]
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class PomodoroModel : BaseModel
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("taskSid")]
	public string TaskSid { get; set; }

	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("startTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime StartTime { get; set; }

	[JsonProperty("endTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime EndTime { get; set; }

	[JsonProperty("adjustTime")]
	public long? AdjustTime { get; set; }

	[JsonIgnore]
	[Ignore]
	public bool IsAdjust
	{
		get
		{
			if (AdjustTime.HasValue)
			{
				return AdjustTime >= Constants.MinFocusDuration * 1000;
			}
			return false;
		}
	}

	[JsonIgnore]
	[NotNull]
	[DefaultValue("2")]
	public int SyncStatus { get; set; }

	[JsonProperty("etag")]
	public string Etag { get; set; }

	[JsonProperty("userId")]
	public string UserId { get; set; }

	[Ignore]
	[JsonProperty("tasks")]
	public PomoTask[] Tasks { get; set; }

	[JsonProperty("added")]
	public bool Added { get; set; }

	[JsonProperty("type")]
	public int Type { get; set; }

	[JsonProperty("pauseDuration")]
	public long PauseDuration { get; set; }

	[JsonProperty("note")]
	public string Note { get; set; }
}
