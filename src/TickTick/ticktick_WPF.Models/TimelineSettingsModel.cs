using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Views.Timeline;

namespace ticktick_WPF.Models;

public class TimelineSettingsModel : PreferenceBaseModel
{
	[JsonProperty("color")]
	public string Color { get; set; } = TimelineColorType.List.GetName();

	[JsonProperty("showWeek")]
	public bool ShowWeek { get; set; }

	[JsonProperty("showGuide")]
	public bool ShowGuide { get; set; } = true;

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is TimelineSettingsModel timelineSettingsModel)
		{
			if (timelineSettingsModel.mtime > base.mtime)
			{
				Color = timelineSettingsModel.Color;
				ShowWeek = timelineSettingsModel.ShowWeek;
				ShowGuide = timelineSettingsModel.ShowGuide;
				base.mtime = timelineSettingsModel.mtime;
				DataChangedNotifier.NotifyTimelineSettingsChanged();
				UtilLog.Info("TimelineSettingsModel.SetRemoteValue");
			}
			else if (timelineSettingsModel.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
