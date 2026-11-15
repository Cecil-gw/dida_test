using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync;

public class MoveOrRestoreProject
{
	[JsonProperty(PropertyName = "fromProjectId")]
	public string FromProjectId { get; set; }

	[JsonProperty(PropertyName = "toProjectId")]
	public string ToProjectId { get; set; }

	[JsonProperty(PropertyName = "taskId")]
	public string TaskId { get; set; }
}
