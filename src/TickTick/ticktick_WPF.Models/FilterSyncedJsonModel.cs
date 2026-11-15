using TickTickModels;

namespace ticktick_WPF.Models;

public class FilterSyncedJsonModel : BaseModel
{
	public string FilterId { get; set; }

	public string UserId { get; set; }

	public string JsonString { get; set; }
}
