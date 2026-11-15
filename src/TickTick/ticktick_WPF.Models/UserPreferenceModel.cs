using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class UserPreferenceModel
{
	public MatrixModel matrix { get; set; }

	[JsonProperty("timeline")]
	public TimelineSettingsModel Timeline { get; set; }

	public DesktopConfigModel desktop_conf { get; set; }

	public DesktopTabBar desktopTabBars { get; set; }

	[JsonProperty("timetable")]
	public TimeTableModel TimeTable { get; set; }

	[JsonProperty("smartProjectsOption")]
	public SmartProjectsOption SmartProjectsOption { get; set; }

	[JsonProperty("general_conf")]
	public GeneralConfig GeneralConfig { get; set; }

	[JsonProperty("teamConf")]
	public TeamConfig TeamConfig { get; set; }

	[JsonProperty("focusConf")]
	public FocusConfig FocusConfig { get; set; }

	[JsonProperty("countdownConf")]
	public CountdownConfig CountdownConfig { get; set; }

	[JsonProperty("recentlyColors")]
	public RecentlyColors RecentlyColors { get; set; }

	[JsonProperty("summaryTemplates")]
	public SummaryTemplates summaryTemplates { get; set; }

	[JsonProperty("alternativeCalendar")]
	public AlternativeCalendar alternativeCalendar { get; set; }

	[JsonProperty("habitConf")]
	public HabitConfig HabitConfig { get; set; }

	[JsonProperty("desktopQuickAdd")]
	public DesktopQuickAdd desktopQuickAdd { get; set; }

	public GuideShowView desktopSplitView { get; set; }

	public CalTimelineTimeZones timelineTimeZones { get; set; }

	[JsonProperty("calendarColorConfig")]
	public CalendarColorConfig CalendarColorConfig { get; set; }

	[JsonProperty("calendarConfig")]
	public CalendarConfig CalendarConfig { get; set; }

	[JsonProperty("reminder")]
	public ReminderConfig Reminder { get; set; }

	public long mtime { get; set; }

	public long GetMaxMTime()
	{
		return new List<long>
		{
			mtime,
			matrix.mtime,
			Timeline.mtime,
			desktop_conf.mtime,
			TimeTable?.mtime ?? 0,
			GeneralConfig?.mtime ?? 0,
			desktopTabBars?.mtime ?? 0,
			summaryTemplates?.mtime ?? 0
		}.Max();
	}
}
