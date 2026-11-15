using TickTickModels;

namespace ticktick_WPF.Models;

public class ProjectSyncedJsonModel : BaseModel
{
	public string ProjectId { get; set; }

	public string UserId { get; set; }

	public string JsonString { get; set; }
}
