using System;
using Newtonsoft.Json;

namespace TickTickModels;

public class FocusTaskModel
{
	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	public DateTime startTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	public DateTime? endTime { get; set; }

	public int? type { get; set; }

	public string title { get; set; }

	public string id { get; set; }
}
