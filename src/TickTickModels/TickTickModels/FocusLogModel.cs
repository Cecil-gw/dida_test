using System;
using Newtonsoft.Json;

namespace TickTickModels;

public class FocusLogModel
{
	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	public DateTime time { get; set; }

	public int? type { get; set; }

	public string id { get; set; }

	public string title { get; set; }
}
