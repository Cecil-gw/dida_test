using TickTickModels;

namespace ticktick_WPF.Models;

public class PaymentModelV2 : BaseModel
{
	public string orderId { get; set; }

	public string form { get; set; }
}
