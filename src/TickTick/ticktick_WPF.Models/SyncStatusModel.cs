using TickTickModels;

namespace ticktick_WPF.Models;

public class SyncStatusModel : BaseModel
{
	public string UserId { get; set; }

	public string EntityId { get; set; }

	public string MoveFromId { get; set; }

	public string OldParentId { get; set; }

	public int Type { get; set; }

	public long ModifyPoint { get; set; }
}
