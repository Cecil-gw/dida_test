using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class SubscribeInfoModel
{
	public string id { get; set; }

	public string subscribeType { get; set; }

	public string freq { get; set; }

	public string price { get; set; }

	public string status { get; set; }

	public bool paid { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime proEndDate { get; set; }
}
