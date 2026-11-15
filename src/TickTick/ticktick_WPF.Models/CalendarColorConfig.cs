using Newtonsoft.Json;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Models;

public class CalendarColorConfig : PreferenceBaseModel
{
	[JsonProperty("task")]
	public CalendarColorConfigItem Task { get; set; }

	[JsonProperty("habit")]
	public CalendarColorConfigItem Habit { get; set; }

	[JsonProperty("countdown")]
	public CalendarColorConfigItem Countdown { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel remote)
	{
		if (remote is CalendarColorConfig calendarColorConfig)
		{
			if (calendarColorConfig.mtime > base.mtime)
			{
				Task = calendarColorConfig.Task;
				Habit = calendarColorConfig.Habit;
				Countdown = calendarColorConfig.Countdown;
				if (!string.IsNullOrEmpty(Task?.ColorType))
				{
					LocalSettings.Settings.CellColorType = Task.ColorType;
				}
				base.mtime = calendarColorConfig.mtime;
			}
			if (remote.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}
}
