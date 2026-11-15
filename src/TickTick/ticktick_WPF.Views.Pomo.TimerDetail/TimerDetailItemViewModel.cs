using ticktick_WPF.Views.Pomo.FocusStatistics;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerDetailItemViewModel : FocusStatisticsPanelItemViewModel
{
	public bool TimerTitle { get; set; }

	public bool TimerOverview { get; set; }

	public bool TimerTimeline { get; set; }
}
