using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Widget;

public class HabitItemWidgetViewModel
{
	public string Id { get; set; }

	public string Name { get; set; }

	public bool IsTextIcon { get; set; }

	public ImageSource Image { get; set; }

	public string IconText { get; set; }

	public SolidColorBrush Background { get; set; }

	public SolidColorBrush Foreground { get; set; }

	public string Days { get; set; }

	public string DaysUnit { get; set; }

	public string GoalDescribe { get; set; }

	public bool ShowProgress { get; set; }

	public bool HasGoal { get; set; }

	public HabitDayCheckModel DayCheckModel { get; set; }

	public HabitModel Habit { get; set; }
}
