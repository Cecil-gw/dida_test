using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Views.Detail;

public class TaskStickyViewModel : BaseViewModel
{
	public readonly TaskBaseViewModel SourceModel;

	private bool _editting;

	public string Title => SourceModel.Title;

	public string StickyTitle
	{
		get
		{
			if (!string.IsNullOrEmpty(SourceModel.Title))
			{
				return SourceModel.Title;
			}
			return Utils.GetString("StickyNote");
		}
	}

	public int Status => SourceModel.Status;

	public string Kind => SourceModel.Kind;

	public bool Enable => SourceModel.Editable;

	public string Id => SourceModel.Id;

	public string ToastString { get; set; }

	public TaskStickyWindow Window { get; set; }

	public string DateText
	{
		get
		{
			if (SourceModel.StartDate.HasValue)
			{
				return DateUtils.FormatDateString(SourceModel.StartDate.Value, SourceModel.DueDate, SourceModel.IsAllDay);
			}
			if (SourceModel.Status != 0)
			{
				return Utils.GetString("DateNotSet");
			}
			if (!(Kind == "NOTE"))
			{
				return Utils.GetString("DateAndReminder");
			}
			return Utils.GetString("SetReminder");
		}
	}

	public string RepeatText
	{
		get
		{
			string repeatFlag = SourceModel.RepeatFlag;
			if (RepeatUtils.GetRepeatType(SourceModel.RepeatFrom, repeatFlag) == RepeatFromType.Custom)
			{
				return Utils.GetString("RepeatByCustom");
			}
			if (string.IsNullOrEmpty(repeatFlag) || repeatFlag == "RRULE:FREQ=NONE")
			{
				return string.Empty;
			}
			if (!repeatFlag.Contains("FREQ=DAILY") || !repeatFlag.Contains("TT_SKIP=HOLIDAY,WEEKEND") || (repeatFlag.Contains("INTERVAL") && !repeatFlag.Contains("INTERVAL=1")))
			{
				return RRuleUtils.RRule2String(SourceModel.RepeatFrom, repeatFlag, SourceModel.StartDate, showUntil: false);
			}
			return Utils.GetString("OfficialWorkingDays");
		}
	}

	public TaskStickyViewModel(string taskId)
	{
		TaskBaseViewModel sourceModel = TaskCache.GetTaskById(taskId) ?? new TaskBaseViewModel
		{
			Id = taskId
		};
		SourceModel = sourceModel;
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("DateText");
			OnPropertyChanged("Kind");
		}, "Kind");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("Title");
			OnPropertyChanged("StickyTitle");
		}, "Title");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("DateText");
			OnPropertyChanged("RepeatText");
		}, "StartDate");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("RepeatText");
		}, "RepeatFlag");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("RepeatText");
		}, "RepeatFrom");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("DateText");
		}, "DueDate");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("DateText");
		}, "IsAllDay");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("Enable");
		}, "Editable");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("Status");
		}, "Status");
		PropertyChangedEventManager.AddHandler(SourceModel, delegate
		{
			OnPropertyChanged("Deleted");
		}, "Deleted");
	}

	public async void CompleteTaskCommand()
	{
		if (!(Kind == "NOTE"))
		{
			int closeStatus = ((Status == 0) ? 2 : 0);
			if ((await TaskService.SetTaskStatus(SourceModel.Id, closeStatus, needUndo: false, null, inDetail: true))?.RepeatTask != null)
			{
				TryToast(Utils.GetString("CreatedTheNextCircle"));
			}
		}
	}

	public async Task ShowDateSelector(FrameworkElement sender)
	{
		TaskModel task = await TaskDao.GetThinTaskById(SourceModel.Id);
		if (task != null)
		{
			List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
			TimeData timeData = new TimeData
			{
				StartDate = task.startDate,
				DueDate = task.dueDate,
				IsAllDay = task.isAllDay,
				Reminders = reminders,
				RepeatFrom = task.repeatFrom,
				RepeatFlag = task.repeatFlag,
				AnnoyingAlert = task.annoyingAlert
			};
			if (!string.IsNullOrEmpty(task.timeZone))
			{
				timeData.TimeZone = new TimeZoneViewModel(task.Floating, task.timeZone);
			}
			if (timeData.IsAllDay.HasValue && timeData.IsAllDay.Value && timeData.StartDate.HasValue && timeData.DueDate.HasValue && timeData.StartDate.Value.Date == timeData.DueDate.Value.Date)
			{
				timeData.DueDate = null;
			}
			if (timeData.StartDate.HasValue)
			{
				timeData.IsDefault = false;
			}
			bool canSkip = !TaskService.IsNonRepeatTask(await TaskDao.GetThinTaskById(SourceModel.Id));
			TaskStickyWindow window = sender as TaskStickyWindow;
			bool isNote = task.kind == "NOTE";
			SetDateDialog dialog = SetDateDialog.GetDialog();
			dialog.ClearEventHandle();
			dialog.Clear += async delegate
			{
				await ClearTaskDate();
			};
			dialog.Save += async delegate(object obj, TimeData model)
			{
				TaskService.TryFixRepeatFlag(model);
				await SaveTaskDate(model);
			};
			dialog.Hided += delegate
			{
				window?.Activate();
				window?.SetEditing(editing: false);
			};
			dialog.SkipRecurrence += OnSkipRecurrence;
			dialog.Show(timeData, new SetDateDialogArgs(calendarMode: false, isNote, itemMode: false, window?.BottomGrid, -20.0, 25.0, PlacementMode.Relative, showQuickDate: true, canSkip));
			window?.SetEditing(editing: true);
		}
	}

	private void OnSkipRecurrence(object sender, EventArgs e)
	{
		TaskService.SkipCurrentRecurrence(SourceModel.Id, notify: true, Window);
	}

	private async Task SaveTaskDate(TimeData model)
	{
		CheckIfReminderPassed(model);
		await TaskService.SetDate(SourceModel.Id, model);
	}

	private void CheckIfReminderPassed(TimeData model)
	{
		if (model != null && model.IsAllDay.HasValue && !model.IsAllDay.Value && model.StartDate.HasValue)
		{
			List<TaskReminderModel> reminders = model.Reminders;
			if (reminders != null && reminders.Count > 0 && (!(model.StartDate.Value >= DateTime.Now) || model.Reminders.Count((TaskReminderModel r) => !r.trigger.Contains("END")) <= 0) && (!model.DueDate.HasValue || !(model.DueDate.Value >= DateTime.Now) || model.Reminders.Count((TaskReminderModel r) => r.trigger.Contains("END")) <= 0))
			{
				TryToast(Utils.GetString("InvalidReminder"));
			}
		}
	}

	private async Task ClearTaskDate()
	{
		await TaskService.ClearDate(SourceModel.Id);
	}

	private void TryToast(string getString)
	{
		ToastString = getString;
		OnPropertyChanged("ToastString");
	}

	public bool IsEmptyTask()
	{
		if (Kind != "CHECKLIST" && string.IsNullOrEmpty(Title))
		{
			return string.IsNullOrEmpty(SourceModel.Content);
		}
		return false;
	}

	public void ResetDateText()
	{
		OnPropertyChanged("DateText");
	}
}
