using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusStatisticsPanelItemViewModel : BaseViewModel
{
	public bool IsTitle { get; set; }

	public bool IsOverview { get; set; }

	public bool IsRecord { get; set; }

	public bool IsLoadMore { get; set; }

	public bool IsEmpty { get; set; }
}
