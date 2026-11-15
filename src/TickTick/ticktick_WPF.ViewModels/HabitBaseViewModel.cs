using System;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;

namespace ticktick_WPF.ViewModels;

public class HabitBaseViewModel : TaskBaseViewModel
{
	public HabitModel Habit { get; set; }

	public HabitCheckInModel HabitCheckIn { get; set; }

	public HabitBaseViewModel(HabitModel habit)
	{
		base.Id = habit.Id;
		base.Type = DisplayType.Habit;
		base.Title = habit.Name;
		base.Priority = 0;
		base.Status = 0;
		Habit = habit;
		base.SortOrder = habit.SortOrder;
		base.StartDate = DateTime.Today;
		base.IsAllDay = true;
		base.ReminderString = habit.Reminder;
		base.Color = habit.Color;
		base.ParentOrder = HabitSectionCache.GetSectionById(Habit.SectionId)?.SortOrder ?? 0;
	}
}
