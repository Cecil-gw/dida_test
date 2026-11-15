using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Util.Sync.ClosedLoader;

public class ClosedLoadStatus : BaseModel
{
	public string UserId { get; set; }

	public string EntityId { get; set; }

	public string TagToken { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime EarliestPulledDate { get; set; }

	public DateTime LastPullTime { get; set; }

	public bool IsAllLoaded { get; set; }

	[Ignore]
	public bool IsNew { get; set; }
}
