using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class DayCellViewModel : BaseHidableViewModel
{
	public int Row { get; set; }

	public int Column { get; set; }

	public HabitDayViewModel Data { get; set; }
}
