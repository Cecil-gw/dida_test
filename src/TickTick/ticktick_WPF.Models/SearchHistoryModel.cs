using TickTickModels;

namespace ticktick_WPF.Models;

public class SearchHistoryModel : BaseModel
{
	public string userId { get; set; }

	public string keyText { get; set; }

	public string tags { get; set; }

	public long sortOrder { get; set; }
}
