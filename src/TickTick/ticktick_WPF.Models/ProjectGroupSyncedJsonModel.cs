using TickTickModels;

namespace ticktick_WPF.Models;

public class ProjectGroupSyncedJsonModel : BaseModel
{
	public string GroupId { get; set; }

	public string UserId { get; set; }

	public string JsonString { get; set; }
}
