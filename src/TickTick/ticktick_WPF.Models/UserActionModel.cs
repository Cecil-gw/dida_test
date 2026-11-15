using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class UserActionModel
{
	public string kind { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime time { get; set; }

	public string platform { get; set; } = "windows";

	public string did { get; set; }

	public string id { get; set; }

	public string userCode { get; set; }

	public string type { get; set; }

	public string ctype { get; set; }

	public object data { get; set; }
}
