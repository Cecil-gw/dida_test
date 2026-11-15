using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class ConferenceModel : BaseModel
{
	[JsonProperty("entryPoints")]
	public List<EntryPointsModel> EntryPoints { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }
}
