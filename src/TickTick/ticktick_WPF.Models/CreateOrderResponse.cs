using TickTickModels;

namespace ticktick_WPF.Models;

public class CreateOrderResponse : BaseModel
{
	public string errCode { get; set; }

	public string orderId { get; set; }

	public string form { get; set; }

	public string redirectUrl { get; set; }
}
