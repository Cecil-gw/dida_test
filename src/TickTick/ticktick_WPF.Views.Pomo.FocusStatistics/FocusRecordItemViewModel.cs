using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Pomo.FocusStatistics;

public class FocusRecordItemViewModel : FocusStatisticsPanelItemViewModel, INotifyPropertyChanged
{
	private string _note;

	public bool _showTitle { get; set; }

	public string DateText { get; set; }

	public int Type { get; set; }

	public DateTime StartTime { get; set; }

	public DateTime EndTime { get; set; }

	public string Note
	{
		get
		{
			return _note;
		}
		set
		{
			_note = value;
			NotifyPropertyChanged("Note");
		}
	}

	public bool IsLastItem { get; set; }

	public ObservableCollection<string> Titles { get; set; } = new ObservableCollection<string>();

	public HashSet<int> UnlinkedTitleIndexes { get; } = new HashSet<int>();

	public string FocusId { get; set; }

	public bool Editable { get; set; }

	public long PauseDuration { get; set; }

	public DateTime Date { get; set; }

	public string TimeText => DateUtils.GetTimeText(StartTime) + " - " + DateUtils.GetTimeText(EndTime);

	public string DurationText => Utils.GetShortDurationString(Math.Max(0L, (long)(EndTime - StartTime).TotalSeconds - PauseDuration), showZeroMinute: false);

	public new event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public FocusRecordItemViewModel()
	{
		base.IsRecord = true;
	}

	public FocusRecordItemViewModel(DateTime dateTime)
		: this()
	{
		Date = dateTime;
		if (dateTime.Year == DateTime.Today.Year)
		{
			DateText = DateUtils.FormatMonthDay(dateTime, DateFormatLen.Middle);
		}
		else
		{
			DateText = DateUtils.FormatYearMonthDay(dateTime, DateFormatLen.Middle);
		}
	}

	public FocusRecordItemViewModel(PomodoroModel focus, DateTime dateTime, IEnumerable<PomoTask> tasks, bool showTitle = true)
		: this()
	{
		_showTitle = showTitle;
		FocusId = focus.Id;
		Type = focus.Type;
		Titles = new ObservableCollection<string>();
		Date = dateTime.Date;
		DateTime date = Date;
		DateTime dateTime2 = date.AddDays(1.0);
		List<PomoTask> list = tasks?.ToList() ?? focus.Tasks?.ToList() ?? new List<PomoTask>();
		focus.Tasks = list.ToArray();
		StartTime = ((focus.StartTime > date) ? focus.StartTime : date);
		EndTime = ((focus.EndTime < dateTime2) ? focus.EndTime : dateTime2);
		if (EndTime < StartTime)
		{
			EndTime = StartTime;
		}
		Note = focus.Note;
		long focusDurationInRange = PomoService.GetFocusDurationInRange(focus, date, dateTime2);
		long num = (long)Math.Max(0.0, (EndTime - StartTime).TotalSeconds);
		PauseDuration = Math.Max(0L, num - focusDurationInRange);
		Editable = (DateTime.Today - focus.StartTime.Date).TotalDays <= 30.0;
		if (showTitle)
		{
			ResetTitles(list, date, dateTime2);
		}
	}

	public async Task ResetTitle()
	{
		PomodoroModel pomodoroModel = await PomoService.GetAdjustPomo(FocusId);
		Note = pomodoroModel?.Note;
		if (pomodoroModel != null && _showTitle)
		{
			if (Titles == null)
			{
				Titles = new ObservableCollection<string>();
			}
			DateTime date = Date.Date;
			DateTime rangeEnd = date.AddDays(1.0);
			string text = string.Join(",", Titles);
			ResetTitles(pomodoroModel.Tasks ?? Array.Empty<PomoTask>(), date, rangeEnd);
			string text2 = string.Join(",", Titles);
			if (text != text2)
			{
				PomoNotifier.NotifyChanged(FocusId, PomoChangeType.Updated);
			}
		}
	}

	private void ResetTitles(IEnumerable<PomoTask> tasks, DateTime rangeStart, DateTime rangeEnd)
	{
		if (Titles == null)
		{
			ObservableCollection<string> observableCollection = (Titles = new ObservableCollection<string>());
		}
		Titles.Clear();
		UnlinkedTitleIndexes.Clear();
		DateTime visibleStart = ((StartTime > rangeStart) ? StartTime : rangeStart);
		DateTime visibleEnd = ((EndTime < rangeEnd) ? EndTime : rangeEnd);
		List<PomoTask> list = (from task in tasks ?? Enumerable.Empty<PomoTask>()
			where IsTaskInRange(task, visibleStart, visibleEnd) && task.TaskId != "StartPause"
			orderby task.StartTime, task.EndTime
			select task).ToList();
		if (!list.Any((PomoTask task) => !string.IsNullOrEmpty(task.GetId())))
		{
			return;
		}
		string text = Utils.GetString("FocusNotSetTask");
		foreach (PomoTask item in list)
		{
			string text2 = item.GetTitle();
			bool flag = false;
			if (string.IsNullOrEmpty(text2))
			{
				if (!string.IsNullOrEmpty(item.GetId()))
				{
					continue;
				}
				text2 = text;
				flag = true;
			}
			if (Titles.LastOrDefault() != text2)
			{
				Titles.Add(text2);
				if (flag)
				{
					UnlinkedTitleIndexes.Add(Titles.Count - 1);
				}
			}
		}
	}

	private static bool IsTaskInRange(PomoTask task, DateTime rangeStart, DateTime rangeEnd)
	{
		if (task != null && task.StartTime < rangeEnd)
		{
			return task.EndTime > rangeStart;
		}
		return false;
	}
}
