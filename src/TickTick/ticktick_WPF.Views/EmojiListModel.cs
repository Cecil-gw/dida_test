using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Views;

public class EmojiListModel
{
	[JsonProperty("key")]
	public string Key { get; set; }

	[JsonProperty("items")]
	public List<EmojiItemModel> Items { get; set; }
}
