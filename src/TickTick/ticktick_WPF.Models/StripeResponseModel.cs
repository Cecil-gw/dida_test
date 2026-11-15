using TickTickModels;

namespace ticktick_WPF.Models;

public class StripeResponseModel : BaseModel
{
	public string sessionId { get; set; }

	public string orderId { get; set; }
}
