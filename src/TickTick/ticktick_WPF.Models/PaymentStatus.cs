using TickTickModels;

namespace ticktick_WPF.Models;

public class PaymentStatus : BaseModel
{
	public string status { get; set; }

	public long endDate { get; set; }
}
