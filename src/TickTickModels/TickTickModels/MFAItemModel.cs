using System;
using Newtonsoft.Json;

namespace TickTickModels;

public class MFAItemModel
{
	public bool enabled { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? ctime { get; set; }

	public string name { get; set; }

	public string secret { get; set; }
}
