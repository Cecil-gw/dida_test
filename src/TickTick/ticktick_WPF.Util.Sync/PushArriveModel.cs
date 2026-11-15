using System;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Util.Sync;

public class PushArriveModel
{
	public string id;

	public string model;

	public string osVersion;

	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime time;
}
