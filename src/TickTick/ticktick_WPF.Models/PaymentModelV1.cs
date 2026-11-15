using TickTickModels;

namespace ticktick_WPF.Models;

public class PaymentModelV1 : BaseModel
{
	public string orderId { get; set; }

	public string redirectUrl { get; set; }
}
