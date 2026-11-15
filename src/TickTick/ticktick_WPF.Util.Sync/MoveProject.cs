using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync;

public class MoveProject : MoveOrRestoreProject
{
	[JsonProperty(PropertyName = "sortOrder")]
	public long SortOrder { get; set; }
}
