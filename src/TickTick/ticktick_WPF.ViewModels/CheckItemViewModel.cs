using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.ViewModels;

[Serializable]
public class CheckItemViewModel : BaseViewModel
{
	private bool _showAddHint;

	private bool _showBottomDropLine;

	private bool _showTopDropLine;

	public TaskBaseViewModel SourceViewModel { get; set; }

	public double RepeatDiff { get; set; }

	public string Id => SourceViewModel.Id;

	public string TaskServerId => SourceViewModel.ParentId;

	public bool ShowDragHandle => SourceViewModel.Status == 0;

	public bool Enable { get; set; } = true;

	public bool IsAgendaOwner { get; set; } = true;

	public string TimeZoneName
	{
		get
		{
			if (SourceViewModel.OwnerTask != null && !(SourceViewModel.OwnerTask.IsAllDay ?? true))
			{
				return SourceViewModel.OwnerTask.TimeZoneName;
			}
			return TimeZoneData.LocalTimeZoneModel?.TimeZoneName;
		}
	}

	public bool IsFloating
	{
		get
		{
			if (SourceViewModel.OwnerTask != null && !(SourceViewModel.OwnerTask.IsAllDay ?? true))
			{
				return SourceViewModel.OwnerTask.IsFloating;
			}
			return false;
		}
	}

	private bool _isValid { get; set; }

	public string Title => SourceViewModel.Title;

	public int Status => SourceViewModel.Status;

	public long SortOrder => SourceViewModel.SortOrder;

	public DateTime? CompletedTime => SourceViewModel.CompletedTime;

	public DateTime? StartDate => SourceViewModel.StartDate;

	public DateTime? DisplayStartDate => StartDate?.AddDays(RepeatDiff);

	public DateTime? SnoozeReminderTime => SourceViewModel.RemindTime;

	public bool? IsAllDay => SourceViewModel.IsAllDay;

	public bool ShowTopDropLine
	{
		get
		{
			return _showTopDropLine;
		}
		set
		{
			_showTopDropLine = value;
			OnPropertyChanged("ShowTopDropLine");
		}
	}

	public bool ShowBottomDropLine
	{
		get
		{
			return _showBottomDropLine;
		}
		set
		{
			_showBottomDropLine = value;
			OnPropertyChanged("ShowBottomDropLine");
		}
	}

	public bool ShowAddHint
	{
		get
		{
			return _showAddHint;
		}
		set
		{
			_showAddHint = value;
			OnPropertyChanged("HintVisible");
		}
	}

	public bool HintVisible
	{
		get
		{
			if (ShowAddHint)
			{
				return string.IsNullOrEmpty(Title);
			}
			return false;
		}
	}

	public bool IsValid
	{
		get
		{
			return _isValid;
		}
		set
		{
			_isValid = value;
			OnPropertyChanged("IsValid");
		}
	}

	public CheckItemViewModel()
	{
	}

	public CheckItemViewModel(TaskBaseViewModel vm, TaskDetailViewModel task = null)
	{
		SetSourceModel(vm);
		RepeatDiff = (task?.RepeatDiff).GetValueOrDefault();
		IsValid = true;
	}

	public CheckItemViewModel Clone()
	{
		return new CheckItemViewModel
		{
			SourceViewModel = SourceViewModel
		};
	}

	public static List<CheckItemViewModel> BuildListFromModels(List<TaskDetailItemModel> models, TaskDetailViewModel task)
	{
		List<CheckItemViewModel> list = new List<CheckItemViewModel>();
		if (models != null && models.Count > 0)
		{
			list.AddRange(models.Select((TaskDetailItemModel model) => new CheckItemViewModel(new TaskBaseViewModel
			{
				Id = model.id,
				Title = model.title,
				SortOrder = model.sortOrder,
				CompletedTime = model.completedTime,
				Type = DisplayType.CheckItem,
				IsAllDay = model.isAllDay,
				RemindTime = model.snoozeReminderTime
			}, task)));
		}
		return list;
	}

	public TaskDetailItemModel ToItemModel()
	{
		return new TaskDetailItemModel
		{
			id = Id,
			TaskServerId = TaskServerId,
			status = Status,
			sortOrder = SortOrder,
			completedTime = CompletedTime,
			startDate = StartDate,
			isAllDay = IsAllDay,
			title = Title,
			snoozeReminderTime = SnoozeReminderTime
		};
	}

	public async Task Save(bool checkCount = true)
	{
		await TaskDetailItemDao.SaveChecklistItem(ToItemModel(), updateCache: false, checkCount);
	}

	public async Task Insert()
	{
		await TaskDetailItemDao.InsertChecklistItem(ToItemModel());
	}

	public void SetSourceModel(TaskBaseViewModel vm)
	{
		SourceViewModel = vm;
		if (SourceViewModel != null)
		{
			PropertyChangedEventManager.AddHandler(SourceViewModel, delegate
			{
				OnPropertyChanged("Title");
				OnPropertyChanged("HintVisible");
			}, "Title");
			PropertyChangedEventManager.AddHandler(SourceViewModel, delegate
			{
				OnPropertyChanged("Status");
			}, "Status");
			PropertyChangedEventManager.AddHandler(SourceViewModel, delegate
			{
				OnPropertyChanged("DisplayStartDate");
			}, "StartDate");
			PropertyChangedEventManager.AddHandler(SourceViewModel, delegate
			{
				OnPropertyChanged("SortOrder");
			}, "SortOrder");
			PropertyChangedEventManager.AddHandler(SourceViewModel, delegate
			{
				OnPropertyChanged("IsAllDay");
			}, "IsAllDay");
			PropertyChangedEventManager.AddHandler(SourceViewModel, delegate
			{
				OnPropertyChanged("SnoozeReminderTime");
			}, "RemindTime");
		}
	}
}
