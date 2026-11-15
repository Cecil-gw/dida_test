using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class EventArchiveSyncModel
{
	public string title { get; set; }

	public string eventId { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? dueStart { get; set; }
}
