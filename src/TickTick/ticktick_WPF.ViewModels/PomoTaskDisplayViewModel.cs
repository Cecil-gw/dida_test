using System;
using System.Windows.Media;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class PomoTaskDisplayViewModel : BaseViewModel
{
	private string _title;

	private bool _bind;

	public string PomoId { get; set; }

	public string TaskId { get; set; }

	public string HabitId { get; set; }

	public string TimerId { get; set; }

	public DateTime StartTime { get; set; }

	public string DurationString { get; set; }

	public long Duration { get; set; }

	public string TimeString { get; set; }

	public Geometry Icon { get; set; }

	public bool ShowBottomLine { get; set; }

	public bool Enable { get; set; }

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			OnPropertyChanged("Title");
		}
	}

	public bool Bind
	{
		get
		{
			return _bind;
		}
		set
		{
			_bind = value;
			OnPropertyChanged("Bind");
			OnPropertyChanged("BindTask");
		}
	}

	public bool BindTask => !string.IsNullOrEmpty(TaskId);

	public PomoTaskDisplayViewModel()
	{
	}

	public PomoTaskDisplayViewModel(PomoTask pomoTask, PomodoroModel pomo)
	{
		_title = pomoTask.GetTitle();
		PomoId = pomo.Id;
		TaskId = pomoTask.TaskId;
		HabitId = pomoTask.HabitId;
		TimerId = pomoTask.TimerSid;
		StartTime = pomoTask.StartTime;
		Duration = Utils.GetTotalSecond(pomoTask.StartTime, pomoTask.EndTime);
		if (Duration < 60)
		{
			DurationString = $"{Duration}s";
		}
		else
		{
			DurationString = Utils.GetDurationString(Duration, isShort: true, showZero: true, showSpan: false);
		}
		DateTime endTime = pomoTask.EndTime;
		TimeString = DateUtils.FormatShortDate(StartTime) + " " + DateUtils.GetTimeText(StartTime) + " - " + DateUtils.FormatShortDate(endTime) + " " + DateUtils.GetTimeText(endTime);
		Icon = Utils.GetIcon((pomo.Type == 0) ? "IcPomo" : "IcPomoTimer");
		Bind = !string.IsNullOrEmpty(TaskId) || !string.IsNullOrEmpty(HabitId) || !string.IsNullOrEmpty(TimerId) || !string.IsNullOrEmpty(_title);
		if (string.IsNullOrEmpty(_title) && Bind)
		{
			_title = Utils.GetString("NoTitle");
		}
	}

	public string GetId()
	{
		if (!string.IsNullOrEmpty(TimerId))
		{
			return TimerId;
		}
		if (!string.IsNullOrEmpty(TaskId))
		{
			return TaskId;
		}
		string.IsNullOrEmpty(HabitId);
		return HabitId;
	}
}
