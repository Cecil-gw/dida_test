using TickTickModels;

namespace ticktick_WPF.Models;

public class PayModel : BaseModel
{
	public string channel { get; set; } = "windows";

	public string freq { get; set; } = "month";

	public string type { get; set; } = "wxpay";

	public int count { get; set; } = 1;
}
