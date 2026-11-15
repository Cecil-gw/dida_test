using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

public class EntryPointsModel : BaseModel
{
	[JsonProperty("entryPointType")]
	public string EntryPointType { get; set; }

	[JsonProperty("uri")]
	public string Uri { get; set; }
}
