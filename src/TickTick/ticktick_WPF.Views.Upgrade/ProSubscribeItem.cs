namespace ticktick_WPF.Views.Upgrade;

internal class ProSubscribeItem
{
	public string Id { get; set; }

	public bool Paid { get; set; }

	public string Plan { get; set; }

	public string Price { get; set; }

	public string Freq { get; set; }

	public string RenewDate { get; set; }

	public string PayMethod { get; set; }

	public string RawSubsType { get; set; }

	public bool IsShowDetailLink
	{
		get
		{
			if (RawSubsType == "paypal_subscribe" || RawSubsType == "google" || RawSubsType == "stripe_subscribe")
			{
				return !Paid;
			}
			return false;
		}
	}
}
