using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Habit;

namespace ticktick_WPF.ViewModels;

public class HabitItemViewModel : HabitItemBaseViewModel
{
	private bool _showCheckIns;

	private bool _showDateCheckIn;

	private bool _showCompletedCycles;

	private int _currentStreak;

	public HabitModel Habit { get; set; }

	public bool IsSelected { get; set; }

	public override string Id => Habit.Id;

	public string IconText { get; set; }

	public int CompletedCycles
	{
		get
		{
			return Habit.CompletedCycles.GetValueOrDefault();
		}
		set
		{
			Habit.CompletedCycles = value;
			OnPropertyChanged("CompletedCycles");
			OnPropertyChanged("CompletedCyclesDisplayStr");
		}
	}

	public string CompletedCyclesDisplayStr => "";

	public string Name
	{
		get
		{
			return Habit.Name;
		}
		set
		{
			Habit.Name = value;
			OnPropertyChanged("Name");
		}
	}

	public string IconRes
	{
		get
		{
			return Habit.IconRes;
		}
		set
		{
			Habit.IconRes = value;
			OnPropertyChanged("IconUrl");
		}
	}

	public string Color
	{
		get
		{
			return Habit.Color;
		}
		set
		{
			Habit.Color = value;
			OnPropertyChanged("Color");
		}
	}

	public new long SortOrder => Habit.SortOrder;

	public bool ShowDateCheckIn
	{
		get
		{
			if (_showDateCheckIn)
			{
				return !base.Dragging;
			}
			return false;
		}
		set
		{
			_showDateCheckIn = value;
		}
	}

	public HabitDayCheckModel DateCheckIn { get; set; }

	public DateTime? DisplayDate { get; set; }

	public bool ShowCheckIns
	{
		get
		{
			return _showCheckIns;
		}
		set
		{
			_showCheckIns = value;
			OnPropertyChanged("ShowCheckIns");
		}
	}

	public bool ShowCompletedCycles
	{
		get
		{
			return _showCompletedCycles;
		}
		set
		{
			_showCompletedCycles = value;
			OnPropertyChanged("ShowCompletedCycles");
		}
	}

	public int CurrentStreak
	{
		get
		{
			return _currentStreak;
		}
		set
		{
			_currentStreak = value;
			OnPropertyChanged("CurrentStreakDays");
			OnPropertyChanged("CurrentStreak");
			OnPropertyChanged("ShortCurrentStreakDays");
		}
	}

	public string CurrentStreakDays => CurrentStreak + (Utils.IsCn() ? "" : " ") + Utils.GetString((CurrentStreak > 1) ? "PublicUpDays" : "PublicUpDay");

	public string ShortCurrentStreakDays => CurrentStreak + (Utils.IsCn() ? Utils.GetString((CurrentStreak > 1) ? "PublicUpDays" : "PublicUpDay") : "");

	public int TotalCheckIns
	{
		get
		{
			return Habit.TotalCheckIns;
		}
		set
		{
			Habit.TotalCheckIns = value;
			OnPropertyChanged("TotalDays");
			OnPropertyChanged("TotalCheckIns");
			OnPropertyChanged("ShortTotalDays");
		}
	}

	public string TotalDays => TotalCheckIns + (Utils.IsCn() ? "" : " ") + Utils.GetString((TotalCheckIns > 1) ? "PublicUpDays" : "PublicUpDay");

	public string ShortTotalDays => TotalCheckIns + (Utils.IsCn() ? Utils.GetString((TotalCheckIns > 1) ? "PublicUpDays" : "PublicUpDay") : "");

	public override bool Archived => Habit.Status == 1;

	public string IconUrl
	{
		get
		{
			if (IconRes.StartsWith("txt"))
			{
				return null;
			}
			return "../../Assets/Habits/" + IconRes.ToLower() + ".png";
		}
	}

	public bool IsIcon => !IconRes.Contains("txt");

	public ObservableCollection<HabitDayCheckModel> WeekCheckIns { get; set; } = new ObservableCollection<HabitDayCheckModel>();

	public HabitItemViewModel(HabitModel habit, DateTime? displayDate)
	{
		Habit = habit;
		IconText = habit.IconRes.Split('_').LastOrDefault() ?? "";
		DisplayDate = displayDate;
		ShowCheckIns = habit.Status == 0 && !displayDate.HasValue;
		_showDateCheckIn = habit.Status == 0 && displayDate.HasValue;
		ShowCompletedCycles = habit.Status == 1;
		UpdateTotalCheckDaysAndCurrentStreak();
	}

	public async void SetCheckIns(List<DateTime> noNeedCheckDates)
	{
		if (Habit.Status != 0)
		{
			return;
		}
		List<HabitCheckInModel> source = (await HabitCheckinCache.GetHabitCheckInsById(Habit?.Id)) ?? new List<HabitCheckInModel>();
		if (DisplayDate.HasValue)
		{
			HabitCheckInModel checkIn = source.FirstOrDefault((HabitCheckInModel c) => c.CheckinStamp == DisplayDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
			DateCheckIn = new HabitDayCheckModel(checkIn, DisplayDate.Value, Habit, 0);
			OnPropertyChanged("DateCheckIn");
		}
		for (int num = -6; num < 1; num++)
		{
			string stamp = DateTime.Today.AddDays(num).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
			HabitCheckInModel checkIn2 = source.FirstOrDefault((HabitCheckInModel c) => c.CheckinStamp == stamp);
			bool needCheck = noNeedCheckDates == null || !noNeedCheckDates.Contains(DateTime.Today.AddDays(num));
			if (WeekCheckIns.Count > 6 + num)
			{
				WeekCheckIns[6 + num] = new HabitDayCheckModel(checkIn2, DateTime.Today.AddDays(num), Habit, 6 + num)
				{
					NeedCheck = needCheck
				};
			}
			else
			{
				WeekCheckIns.Add(new HabitDayCheckModel(checkIn2, DateTime.Today.AddDays(num), Habit, 6 + num)
				{
					NeedCheck = needCheck
				});
			}
		}
	}

	public async Task UpdateTotalCheckDaysAndCurrentStreak()
	{
		HabitStatisticsModel habitStatisticsModel = await HabitStatisticsUtils.CalculateHabitStatInfo(Habit.Id, Habit.RepeatRule);
		if (habitStatisticsModel == null)
		{
			TotalCheckIns = 0;
			CurrentStreak = 0;
		}
		else
		{
			TotalCheckIns = habitStatisticsModel.TotalCheckIns;
			CurrentStreak = (Archived ? habitStatisticsModel.MaxStreak : habitStatisticsModel.CurrentStreak);
		}
	}

	public void SetCyclesList(List<CompletedCycle> newCycles)
	{
		Habit.CompletedCyclesList = newCycles;
		OnPropertyChanged("TotalDays");
	}

	public HabitItemViewModel Copy()
	{
		HabitItemViewModel obj = (HabitItemViewModel)MemberwiseClone();
		obj.Dragging = false;
		obj.Selected = false;
		return obj;
	}
}
