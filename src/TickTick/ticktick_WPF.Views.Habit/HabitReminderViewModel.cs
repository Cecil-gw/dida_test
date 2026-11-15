using System;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class HabitReminderViewModel : BaseViewModel
{
	public DateTime Time { get; set; }

	public bool IsAdd { get; set; }
}
