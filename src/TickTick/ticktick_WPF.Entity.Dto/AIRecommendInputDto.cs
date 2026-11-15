using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Entity.Dto;

public class AIRecommendInputDto
{
	public string id { get; set; }

	public string title { get; set; }

	public int priority { get; set; }

	public string category { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? startDate { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? dueDate { get; set; }

	public IEnumerable<string> tags { get; set; }

	[JsonIgnore]
	public DateTime? Time { get; set; }
}
