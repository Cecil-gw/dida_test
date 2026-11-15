using TickTickModels;

namespace ticktick_WPF.Models;

public class OrderSpecModel : BaseModel
{
	public string count { get; set; }

	public string unit { get; set; }

	public string amount { get; set; }

	public string strikePrice { get; set; }
}
