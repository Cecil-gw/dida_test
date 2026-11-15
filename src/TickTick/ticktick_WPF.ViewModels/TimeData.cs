using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.ViewModels;

public class TimeData : BaseViewModel
{
	public bool HasTime;

	private DateTime? _startDate;

	private DateTime? _dueDate;

	private bool? _isAllDay = true;

	private List<TaskReminderModel> _reminders = new List<TaskReminderModel>();

	private string _repeatFrom = "2";

	private string _repeatFlag;

	private bool _isDefault = true;

	private TimeZoneViewModel _timezone = TimeZoneData.LocalTimeZoneModel;

	public BatchData BatchData;

	public List<string> ExDates { get; set; }

	public bool ChangedDateOnly
	{
		get
		{
			if (BatchData != null)
			{
				return !BatchData.IsUnified;
			}
			return false;
		}
	}

	public bool IsTimeUnified
	{
		get
		{
			if (BatchData != null)
			{
				return BatchData.IsTimeUnified;
			}
			return true;
		}
	}

	public int? AnnoyingAlert { get; set; }

	public TimeZoneViewModel TimeZone
	{
		get
		{
			return _timezone;
		}
		set
		{
			_timezone = value;
			OnPropertyChanged("TimeZone");
		}
	}

	public DateTime? StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			_startDate = value;
			OnPropertyChanged("StartDate");
		}
	}

	public DateTime? DueDate
	{
		get
		{
			return _dueDate;
		}
		set
		{
			_dueDate = value;
			if (_dueDate < _startDate)
			{
				_dueDate = _startDate;
			}
			OnPropertyChanged("DueDate");
		}
	}

	public List<TaskReminderModel> Reminders
	{
		get
		{
			return _reminders;
		}
		set
		{
			_reminders = value;
			OnPropertyChanged("Reminders");
		}
	}

	public string RepeatFrom
	{
		get
		{
			return _repeatFrom;
		}
		set
		{
			_repeatFrom = value;
			OnPropertyChanged("RepeatFrom");
		}
	}

	public string RepeatFlag
	{
		get
		{
			return _repeatFlag;
		}
		set
		{
			_repeatFlag = value;
			OnPropertyChanged("RepeatFlag");
		}
	}

	public bool? IsAllDay
	{
		get
		{
			return _isAllDay;
		}
		set
		{
			_isAllDay = value;
			OnPropertyChanged("IsAllDay");
		}
	}

	public bool IsDefault
	{
		get
		{
			return _isDefault;
		}
		set
		{
			_isDefault = value;
			OnPropertyChanged("IsDefault");
		}
	}

	public TimeData()
	{
	}

	public TimeData(TaskModel task, List<TaskReminderModel> reminders)
	{
		_startDate = task.startDate;
		_dueDate = task.dueDate;
		_isAllDay = task.isAllDay;
		_repeatFlag = task.repeatFlag;
		_repeatFrom = task.repeatFrom;
		_timezone = new TimeZoneViewModel(task.isFloating == true, task.timeZone);
		ExDates = ExDateSerilizer.ToArray(task.exDates)?.ToList() ?? new List<string>();
		_reminders = reminders;
		AnnoyingAlert = task.annoyingAlert;
	}

	public static TimeData Clone(TimeData original)
	{
		if (original == null)
		{
			return new TimeData();
		}
		return new TimeData
		{
			HasTime = original.HasTime,
			StartDate = original.StartDate,
			DueDate = original.DueDate,
			Reminders = original.Reminders,
			RepeatFrom = original.RepeatFrom,
			RepeatFlag = original.RepeatFlag,
			IsAllDay = original.IsAllDay,
			IsDefault = original.IsDefault,
			ExDates = original.ExDates,
			TimeZone = original.TimeZone,
			BatchData = original.BatchData,
			AnnoyingAlert = original.AnnoyingAlert
		};
	}

	public static TimeData BuildFromDefault(TaskDefaultModel model)
	{
		TimeData timeData = new TimeData();
		DateTime? defaultDate = TaskDefaultModel.GetDefaultDate(model.Date);
		timeData.StartDate = defaultDate;
		if (!string.IsNullOrEmpty(model.AllDayReminders))
		{
			List<TaskReminderModel> reminders = Utils.BuildReminders(model.AllDayReminders.Split(',').ToList());
			timeData.Reminders = reminders;
		}
		return timeData;
	}

	public static TimeData BuildDefaultStartAndEnd()
	{
		TimeData timeData = new TimeData();
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		if (defaultSafely.DateMode == 1)
		{
			timeData.IsAllDay = Constants.DurationValue.IsAllDayValue(defaultSafely.Duration);
			timeData.StartDate = (timeData.IsAllDay.Value ? defaultSafely.GetDefaultDateTime() : new DateTime?(DateUtils.GetNextHour(defaultSafely.GetDefaultDateTime() ?? DateTime.Today)));
			if (timeData.StartDate.HasValue)
			{
				timeData.DueDate = timeData.StartDate.Value.AddMinutes(defaultSafely.Duration);
			}
		}
		else
		{
			timeData.StartDate = defaultSafely.GetDefaultDateTime();
		}
		return timeData;
	}

	public static List<TaskReminderModel> GetDefaultAllDayReminders()
	{
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		List<TaskReminderModel> result = new List<TaskReminderModel>();
		if (!string.IsNullOrEmpty(defaultSafely?.AllDayReminders))
		{
			result = Utils.BuildReminders(defaultSafely.AllDayReminders.Split(',').ToList());
		}
		return result;
	}

	public static List<TaskReminderModel> GetDefaultTimeReminders()
	{
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		List<TaskReminderModel> result = new List<TaskReminderModel>();
		if (!string.IsNullOrEmpty(defaultSafely?.TimeReminders))
		{
			result = Utils.BuildReminders(defaultSafely.TimeReminders.Split(',').ToList());
		}
		return result;
	}

	public static TimeData InitDefaultTime()
	{
		List<TaskReminderModel> reminders = new List<TaskReminderModel>();
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		DateTime? defaultDate = TaskDefaultModel.GetDefaultDate(defaultSafely?.Date);
		if (!string.IsNullOrEmpty(defaultSafely?.AllDayReminders))
		{
			reminders = Utils.BuildReminders(defaultSafely.AllDayReminders.Split(',').ToList());
		}
		return new TimeData
		{
			StartDate = defaultDate,
			IsAllDay = true,
			IsDefault = true,
			Reminders = reminders
		};
	}

	public static TimeData InitDefaultDuration()
	{
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		DateTime nextHour = DateUtils.GetNextHour(DateUtils.GetNextHour(TaskDefaultModel.GetDefaultDate(defaultSafely?.Date) ?? DateTime.Today));
		DateTime value = nextHour.AddHours(1.0);
		List<TaskReminderModel> reminders = new List<TaskReminderModel>();
		if (defaultSafely != null)
		{
			value = nextHour.AddMinutes(defaultSafely.Duration);
			if (!string.IsNullOrEmpty(defaultSafely.TimeReminders))
			{
				reminders = Utils.BuildReminders(defaultSafely.TimeReminders.Split(',').ToList());
			}
		}
		return new TimeData
		{
			StartDate = nextHour,
			DueDate = value,
			IsAllDay = false,
			IsDefault = true,
			Reminders = reminders
		};
	}

	public override string ToString()
	{
		return StartDate?.ToString("yyyyMMdd'-'HHMM", App.Ci) + "," + DueDate?.ToString("yyyyMMdd'-'HHMM", App.Ci) + "," + IsAllDay + "," + RepeatFlag + "," + TimeZone?.TimeZoneName;
	}

	public void SetDate(DateTime date)
	{
		if (StartDate.HasValue)
		{
			date = new DateTime(date.Year, date.Month, date.Day, StartDate.Value.Hour, StartDate.Value.Minute, 0);
			double totalMinutes = (date - StartDate.Value).TotalMinutes;
			StartDate = date;
			DueDate = DueDate?.AddMinutes(totalMinutes);
		}
		else
		{
			StartDate = date;
		}
	}

	public void RemoveEndReminders()
	{
		if (_reminders == null || !_reminders.Any())
		{
			return;
		}
		List<TaskReminderModel> list = _reminders.Where((TaskReminderModel r) => r.trigger.Contains("END")).ToList();
		foreach (TaskReminderModel item in list)
		{
			_reminders.Remove(item);
		}
		if (list.Count > 0)
		{
			OnPropertyChanged("Reminders");
		}
	}

	public void CheckEndReminders()
	{
		if ((_dueDate.HasValue && _startDate.HasValue && !(_dueDate.Value == _startDate.Value)) || _reminders == null || !_reminders.Any())
		{
			return;
		}
		List<TaskReminderModel> list = _reminders.Where((TaskReminderModel r) => r.trigger.Contains("END")).ToList();
		foreach (TaskReminderModel reminder in list)
		{
			_reminders.Remove(reminder);
			reminder.trigger = reminder.trigger.Replace(";RELATED=END", "");
			if (_reminders.All((TaskReminderModel r) => r.trigger != reminder.trigger))
			{
				_reminders.Add(reminder);
			}
		}
		if (list.Count > 0)
		{
			OnPropertyChanged("Reminders");
		}
	}
}
