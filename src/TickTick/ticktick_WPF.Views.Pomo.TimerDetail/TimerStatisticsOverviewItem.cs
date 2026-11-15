using TickTickModels;

namespace ticktick_WPF.Views.Pomo.TimerDetail;

public class TimerStatisticsOverviewItem : TimerDetailItemViewModel
{
	public int TotalDays { get; set; }

	public long TodayDuration { get; set; }

	public long TotalDuration { get; set; }

	public TimerStatisticsOverviewItem(TimerOverviewModel data)
	{
		TotalDays = data?.Days ?? 0;
		TodayDuration = data?.Today ?? 0;
		TotalDuration = data?.Total ?? 0;
		base.TimerOverview = true;
	}
}
