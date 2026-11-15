using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class UserDeviceInfo
{
	public string did { get; set; }

	public string platform { get; set; } = "windows";

	public string ver { get; set; }

	public string channel { get; set; }

	public string os { get; set; }

	public string osv { get; set; }

	public string device { get; set; }

	public string name { get; set; }

	public string campaign { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime ctime { get; set; }

	public string userCode { get; set; }

	public string hl { get; set; }

	public string tz { get; set; }

	public string locale { get; set; }

	public bool enable_tz { get; set; }
}
