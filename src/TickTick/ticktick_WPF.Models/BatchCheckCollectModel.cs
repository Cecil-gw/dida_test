using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class BatchCheckCollectModel
{
	public long point { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime time { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime endTime { get; set; }

	public long checkPoint { get; set; }

	public string errorId { get; set; }

	public bool sync { get; set; }
}
