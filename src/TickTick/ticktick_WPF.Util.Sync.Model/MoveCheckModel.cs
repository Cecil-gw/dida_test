using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Util.Sync.Model;

public class MoveCheckModel
{
	public string id { get; set; }

	public string projectId { get; set; }

	public string etag { get; set; }

	public long etimestamp { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? modifiedTime { get; set; }
}
