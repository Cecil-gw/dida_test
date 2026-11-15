using System;
using Newtonsoft.Json;
using SQLite;

namespace TickTickModels;

public class TimerModel : BaseModel
{
	[JsonProperty("id")]
	[Indexed]
	public string Id { get; set; }

	[JsonProperty("userId")]
	public string UserId { get; set; }

	[JsonProperty("icon")]
	public string Icon { get; set; }

	[JsonProperty("color")]
	public string Color { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("objId")]
	[Indexed]
	public string ObjId { get; set; }

	[JsonProperty("objType")]
	public string ObjType { get; set; }

	[JsonProperty("type")]
	public string Type { get; set; } = "pomodoro";

	[JsonProperty("pomodoroTime")]
	public int PomodoroTime { get; set; }

	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("sortOrder")]
	public long SortOrder { get; set; }

	[JsonIgnore]
	public bool Deleted { get; set; }

	[JsonProperty("createdTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime CreatedTime { get; set; }

	[JsonProperty("modifiedTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime ModifiedTime { get; set; }

	[JsonProperty("archivedTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? ArchivedTime { get; set; }

	[JsonIgnore]
	public int SyncStatus { get; set; }

	[JsonProperty("etag")]
	public string Etag { get; set; }

	[JsonIgnore]
	public string Overview { get; set; }
}
