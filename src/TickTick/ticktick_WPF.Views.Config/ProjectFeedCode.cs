using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Views.Config;

internal class ProjectFeedCode
{
	public string projectId { get; set; }

	public string code { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? createdTime { get; set; }
}
