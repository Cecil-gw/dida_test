using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncTaskParentRes
{
	private Dictionary<string, string> id2error = new Dictionary<string, string>();

	private Dictionary<string, SyncTaskParentEtag> id2etag = new Dictionary<string, SyncTaskParentEtag>();

	[JsonProperty(PropertyName = "id2etag")]
	public Dictionary<string, SyncTaskParentEtag> Id2etag
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
	public Dictionary<string, string> Id2error
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
