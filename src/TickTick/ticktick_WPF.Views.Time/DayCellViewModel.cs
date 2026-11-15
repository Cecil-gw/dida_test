using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Time;

public class DayCellViewModel : BaseHidableViewModel
{
	public int Row { get; set; }

	public int Column { get; set; }

	public DayViewModel Data { get; set; }
}
