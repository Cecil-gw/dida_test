using System;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class CountDownBaseViewModel : TaskBaseViewModel
{
	public CountdownModel Countdown { get; set; }

	public CountDownBaseViewModel(CountdownModel countdown)
	{
		base.Id = countdown.Id;
		Countdown = countdown;
		base.Type = DisplayType.Countdown;
		base.Title = countdown.Name;
		base.Priority = 0;
		base.Status = 0;
		base.SortOrder = countdown.SortOrder;
		DateTime targetDate = countdown.GetTargetDate();
		base.StartDate = targetDate;
		base.IsAllDay = true;
		base.ReminderString = countdown.Reminder;
		base.Editable = false;
		if (countdown.CheckActualCanShowAge(DisplayTimerMode.CountDown))
		{
			base.Title += countdown.GetAge(targetDate);
		}
	}
}
