using TickTickModels;

namespace ticktick_WPF.Models;

public class SubscribeSpecModel : BaseModel
{
	public string type { get; set; }

	public string price { get; set; }

	public string freq { get; set; }
}
