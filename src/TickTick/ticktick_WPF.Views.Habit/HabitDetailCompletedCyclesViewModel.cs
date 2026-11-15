using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class HabitDetailCompletedCyclesViewModel : BaseViewModel
{
	private ImageSource _icon;

	public ImageSource Icon
	{
		get
		{
			return _icon;
		}
		set
		{
			_icon = value;
			OnPropertyChanged("Icon");
		}
	}

	public bool IsCurrentCyclesCompleted { get; set; }

	public string CurrentCyclesStartDate { get; set; }

	public string CurrentCyclesEndDate { get; set; }

	public int CurrentTargetDays { get; set; }

	public int TargetDays { get; set; }

	public int CompleteCycles { get; set; }

	public HabitDetailCompletedCyclesViewModel(HabitModel habit, int cycle)
	{
		if (habit.TargetDays <= 0)
		{
			return;
		}
		TargetDays = habit.TargetDays.GetValueOrDefault();
		CompleteCycles = habit.CompletedCycles.GetValueOrDefault();
		if (habit.CompletedCyclesList == null || habit.CompletedCyclesList.Count == 0 || habit.CompletedCyclesList.Count < cycle)
		{
			CurrentTargetDays = 0;
			CurrentCyclesStartDate = string.Format(Utils.GetString("HabitCompleteNDayLeft"), TargetDays - CurrentTargetDays);
			IsCurrentCyclesCompleted = false;
		}
		else
		{
			if (cycle >= habit.CompletedCycles)
			{
				cycle = habit.CompletedCyclesList.Count - 1;
			}
			if (cycle > habit.CompletedCyclesList.Count - 1 || cycle < 0)
			{
				return;
			}
			CurrentTargetDays = habit.CompletedCyclesList[cycle].Streak;
			if (CurrentTargetDays != TargetDays)
			{
				CurrentCyclesStartDate = string.Format(Utils.GetString("HabitCompleteNDayLeft"), TargetDays - CurrentTargetDays);
			}
			else
			{
				CurrentCyclesStartDate = DateUtils.FormatShortDate(DateUtils.ParseDateTime(habit.CompletedCyclesList[cycle].StartDate.ToString()));
				CurrentCyclesEndDate = " - " + DateUtils.FormatShortDate(DateUtils.ParseDateTime(habit.CompletedCyclesList[cycle].EndDate.ToString()));
			}
			IsCurrentCyclesCompleted = habit.CompletedCyclesList[cycle].isComplete;
		}
		UpdateIcon();
	}

	public void UpdateIcon()
	{
		Icon = (IsCurrentCyclesCompleted ? (Application.Current?.FindResource("HabitCompletedCycles") as Image).Source : (Application.Current?.FindResource("HabitUncompletedCycles") as Image).Source);
	}
}
