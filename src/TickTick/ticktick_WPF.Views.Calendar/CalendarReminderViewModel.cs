using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Calendar;

public class CalendarReminderViewModel : BaseViewModel
{
	public string Id { get; set; }

	public string Summary { get; set; }

	public string Description { get; set; }
}
