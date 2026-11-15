using Newtonsoft.Json;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Models;

public class SmartProjectOption
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("viewMode")]
	public string ViewMode { get; set; }

	[JsonProperty("sortOption")]
	public SortOption SortOption { get; set; }

	[JsonProperty("timeline")]
	public TimelineModel Timeline { get; set; }

	public void SaveRemote(SmartProjectOption remote)
	{
		if (remote == null)
		{
			return;
		}
		SortOption = remote.SortOption;
		ViewMode = remote.ViewMode;
		if (remote.Timeline != null)
		{
			if (Timeline == null)
			{
				Timeline = remote.Timeline;
				Timeline.Range = TimelineConstants.RangeDefault;
			}
			else
			{
				Timeline.sortOption = remote.Timeline.sortOption;
			}
		}
	}
}
