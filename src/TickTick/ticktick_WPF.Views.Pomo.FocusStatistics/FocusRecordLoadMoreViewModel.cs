namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusRecordLoadMoreViewModel : FocusStatisticsPanelItemViewModel
{
	public bool NeedClick { get; set; }

	public FocusRecordLoadMoreViewModel()
	{
		base.IsLoadMore = true;
	}
}
