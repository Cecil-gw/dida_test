using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class PomoFocusRecFilterModel
{
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? beginTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? endTime { get; set; }

	public int? minDuration { get; set; }

	public int? maxDuration { get; set; }

	public PomoFocusRecRelation relation { get; set; }

	public int? pageSize { get; set; }

	public string pageToken { get; set; }
}
