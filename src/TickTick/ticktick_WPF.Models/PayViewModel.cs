using TickTickModels;

namespace ticktick_WPF.Models;

public class PayViewModel : BaseModel
{
	public string OrderByMonth { get; set; }

	public string OrderByYear { get; set; }

	public string AlipaySubsMonthly { get; set; }

	public string StripeSubsMonthly { get; set; }

	public string StripeSubsYearly { get; set; }

	public string PaypalSubsMonthly { get; set; }

	public string PaypalSubsYearly { get; set; }
}
