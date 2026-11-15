using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class MoveColumnArgs : BaseModel
{
	public string columnId { get; set; }

	public string fromProjectId { get; set; }

	public string toProjectId { get; set; }

	public long sortOrder { get; set; }

	public List<string> taskIds { get; set; }

	[JsonIgnore]
	public List<SyncStatusModel> tasks { get; set; }
}
