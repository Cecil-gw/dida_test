using TickTickModels;

namespace ticktick_WPF.Models;

public class TaskSortOrderInPriorityModel : BaseModel
{
	public string UserId { get; set; }

	public string CatId { get; set; }

	public string EntityType { get; set; }

	public string EntityId { get; set; }

	public int Priority { get; set; }

	public long SortOrder { get; set; }

	public int SyncStatus { get; set; }

	public int Deleted { get; set; }
}
