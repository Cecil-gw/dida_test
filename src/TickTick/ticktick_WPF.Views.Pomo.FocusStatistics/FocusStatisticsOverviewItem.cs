using ticktick_WPF.Models;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusStatisticsOverviewItem : FocusStatisticsPanelItemViewModel
{
	public int TodayPomos { get; set; }

	public int TotalPomos { get; set; }

	public long TodayDuration { get; set; }

	public long TotalDuration { get; set; }

	public FocusStatisticsOverviewItem(StatisticsModel model)
	{
		TodayPomos = model?.todayPomoCount ?? 0;
		TotalPomos = model?.totalPomoCount ?? 0;
		TodayDuration = model?.todayPomoDuration ?? 0;
		TotalDuration = model?.totalPomoDuration ?? 0;
		base.IsOverview = true;
	}
}
