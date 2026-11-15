using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Views;

public class EmojiItemModel
{
	[JsonProperty("key")]
	public string Key { get; set; }

	[JsonProperty("skin")]
	public List<string> Skin { get; set; }
}
