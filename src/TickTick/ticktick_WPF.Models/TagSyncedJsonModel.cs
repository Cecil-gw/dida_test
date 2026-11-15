using TickTickModels;

namespace ticktick_WPF.Models;

public class TagSyncedJsonModel : BaseModel
{
	public string TagName { get; set; }

	public string UserId { get; set; }

	public string JsonString { get; set; }
}
