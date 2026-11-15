using TickTickModels;

namespace ticktick_WPF.Models;

public class SubscribePriceInfoModel : BaseModel
{
	public bool isPro { get; set; }

	public bool needSubscribe { get; set; }

	public string subscribeFreq { get; set; }

	public string subscribeType { get; set; }

	public decimal? oldPrice { get; set; }

	public decimal? price { get; set; }

	public string currency { get; set; }
}
