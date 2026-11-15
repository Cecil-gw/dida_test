using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util;

public class EventUpdateResult
{
	private Dictionary<string, Dictionary<string, string>> id2error = new Dictionary<string, Dictionary<string, string>>();

	private Dictionary<string, Dictionary<string, string>> id2etag = new Dictionary<string, Dictionary<string, string>>();

	[JsonProperty(PropertyName = "accountId")]
	public string AccountId { get; set; }

	[JsonProperty(PropertyName = "errorCode")]
	public string ErrorCode { get; set; }

	[JsonProperty(PropertyName = "id2etag")]
	public Dictionary<string, Dictionary<string, string>> Id2etag
	{
		get
		{
			return id2etag;
		}
		set
		{
			id2etag = value;
		}
	}

	[JsonProperty(PropertyName = "id2error")]
	public Dictionary<string, Dictionary<string, string>> Id2error
	{
		get
		{
			return id2error;
		}
		set
		{
			id2error = value;
		}
	}
}
