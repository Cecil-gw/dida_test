using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class ColumnModel : BaseModel
{
	[Indexed]
	public string id { get; set; }

	public string userId { get; set; }

	[Indexed]
	public string projectId { get; set; }

	public string name { get; set; }

	public long? sortOrder { get; set; }

	public int deleted { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? createdTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? modifiedTime { get; set; }

	public string etag { get; set; }

	public string syncStatus { get; set; }

	public string color { get; set; }
}
