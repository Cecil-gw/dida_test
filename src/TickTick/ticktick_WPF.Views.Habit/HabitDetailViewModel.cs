using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Habit;

public class HabitDetailViewModel : BaseViewModel, INotifyPropertyChanged
{
	private bool _isShowGrayBack;

	private int _completeRatio;

	public bool IsShowGrayBack
	{
		get
		{
			return _isShowGrayBack;
		}
		set
		{
			if (_isShowGrayBack != value)
			{
				_isShowGrayBack = value;
				NotifyPropertyChanged("IsShowGrayBack");
			}
		}
	}

	public int TargetDays { get; set; }

	public int CompleteCycles { get; set; }

	public HabitModel Habit { get; set; }

	public string Id { get; set; }

	public string Type { get; set; }

	public bool IsBoolHabit { get; set; }

	public string Unit { get; set; }

	public string Name { get; set; }

	public string ImageUrl { get; set; } = "";

	public double Goal { get; set; }

	public double Value { get; set; }

	public DateTime PivotDate { get; set; }

	public int CompletedRatio
	{
		get
		{
			return _completeRatio;
		}
		set
		{
			_completeRatio = value;
			OnPropertyChanged("CompletedRatio");
		}
	}

	public double TotalCheckInCount { get; set; }

	public HabitCheckInModel TodayCheckIn { get; set; }

	public int Status { get; set; }

	public double Step { get; set; }

	public string AutoAmount { get; set; }

	public string Color { get; set; }

	public string IconText { get; set; } = "";

	public bool UseCardStyle { get; set; }

	public new event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public HabitDetailViewModel(HabitModel habit)
	{
		string safeIconRes = habit.GetSafeIconRes();
		string safeType = habit.GetSafeType();
		string unitText = HabitUtils.GetUnitText(habit.Unit);
		Habit = habit;
		Id = habit.Id;
		Name = habit.Name;
		Type = safeType;
		if (safeIconRes.StartsWith("txt"))
		{
			IconText = safeIconRes.Split('_').LastOrDefault() ?? "";
		}
		else
		{
			ImageUrl = "pack://application:,,,/Assets/Habits/" + safeIconRes.ToLower() + ".png";
		}
		IsBoolHabit = habit.IsBooleanType();
		Unit = unitText;
		Goal = habit.Goal;
		Step = habit.Step;
		AutoAmount = "+" + habit.Step + " " + unitText;
		Color = habit.Color;
		TargetDays = habit.TargetDays.GetValueOrDefault();
		CompleteCycles = habit.TotalCheckIns;
	}
}
