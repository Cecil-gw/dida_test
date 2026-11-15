using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class YearPromoModel
{
	public string url { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime startTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime endTime { get; set; }
}
