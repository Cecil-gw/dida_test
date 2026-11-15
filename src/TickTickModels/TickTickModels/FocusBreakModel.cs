using System;
using Newtonsoft.Json;

namespace TickTickModels;

public class FocusBreakModel
{
	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	public DateTime? startTime { get; set; }

	[JsonConverter(typeof(UtcDateTimeWithMillConverter))]
	public DateTime? endTime { get; set; }

	public long? duration { get; set; }
}
