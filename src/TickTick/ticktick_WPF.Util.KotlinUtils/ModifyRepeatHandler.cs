using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Ical.Net.DataTypes;
using KotlinModels;
using TickTickDao;
using TickTickHandler;
using TickTickUtils;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Detail;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Time;

namespace ticktick_WPF.Util.KotlinUtils;

public static class ModifyRepeatHandler
{
	private static bool _inited;

	private static IToastShowWindow _toastWindow;

	private static int _handlingVersion;

	private static ConcurrentDictionary<string, EditorType> _batchDeleteTypes;

	private static List<TaskDeleteRecurrenceUndoEntity> _batchDeleteUndoModels;

	public static bool DialogShowing { get; set; }

	public static string HandlingTaskId { get; set; }

	public static bool BatchHandle { get; set; }

	public static bool HandlingFirstRecurrence { get; set; }

	public static int? HandlingAnnoyingAlert { get; set; }

	public static bool HandlingAnnoyingAlertChanged { get; set; }

	public static bool HandlingOnlyAnnoyingAlertChanged { get; set; }

	public static EditorType HandlingEditorType { get; set; }

	private static System.DateTime? _recurringStart { get; set; }

	public static TimeData HandleToTime { get; set; }

	static ModifyRepeatHandler()
	{
		HandlingEditorType = EditorType.CANCEL;
		_batchDeleteTypes = new ConcurrentDictionary<string, EditorType>();
		TickTickTask.DeletePomodoroSummariesCallback(DeletePomodoroSummariesFun);
		ModifySingleRepeat.ShowDeleteChoiceDialogCallback(ShowDeleteChoiceDialogFun);
		ModifySingleRepeat.ShowUpdateDateChoiceDialogCallback(ShowUpdateDateChoiceDialogFun);
		ModifySingleRepeat.OnEndDeterminedCallback(OnEndDeterminedFun);
		TickTickHandler.Utils.GetCurrentRemoteUserIdCallback(GetCurrentRemoteUserIdFun);
		TickTickHandler.Utils.GetUserIdCallback(GetUserIdFun);
		ModifySingleRepeat.InitModifyRepeat();
		_inited = true;
	}

	private static string GetUserIdFun()
	{
		return LocalSettings.Settings.LoginUserId;
	}

	private static long GetCurrentRemoteUserIdFun()
	{
		long.TryParse(LocalSettings.Settings.LoginUserId, out var result);
		return result;
	}

	private static void OnEndDeterminedFun(string editotType, List<TASK> taskList, TaskDeletedUndoModel undoModel)
	{
		if (Enum.TryParse<EditorType>(editotType, ignoreCase: true, out var result))
		{
			HandlingEditorType = result;
		}
		List<TASK> list = taskList;
		if (list != null && list.Count > 0)
		{
			Task.Run(async delegate
			{
				if (editotType.ToLower() != "cancel")
				{
					await Task.Delay(300);
					TaskChangeNotifier.NotifyBatchDateChanged(taskList.Select((TASK t) => t.id).ToList());
					GlobalEventManager.NotifyReloadCalendar();
				}
			});
		}
		List<string> list2 = undoModel.deleteSids.ToList();
		if (list2 != null && list2.Any())
		{
			DeleteTask(list2[0]);
		}
		if (!BatchHandle && _toastWindow == null)
		{
			TaskService.BatchDeleteTaskByIds(list2, showUndo: false);
		}
		else
		{
			List<TaskDeleteUndoUpdateEntity> list3 = undoModel.updateEntities.ToList();
			if (list3 != null)
			{
				List<TaskDeleteRecurrenceUndoEntity> list4 = new List<TaskDeleteRecurrenceUndoEntity>();
				foreach (TaskDeleteUndoUpdateEntity item in list3)
				{
					list4.Add(new TaskDeleteRecurrenceUndoEntity(item));
				}
				if (BatchHandle && _batchDeleteUndoModels != null)
				{
					_batchDeleteUndoModels.AddRange(list4);
					return;
				}
				_toastWindow?.ToastDeleteRecUndo(list4);
			}
		}
		string handlingTaskId = HandlingTaskId;
		int handlingVersion = _handlingVersion;
		Task.Run(async delegate
		{
			await Task.Delay(2000);
			if (!(HandlingTaskId != handlingTaskId) && _handlingVersion == handlingVersion)
			{
				HandlingAnnoyingAlert = null;
				HandlingAnnoyingAlertChanged = false;
				HandlingOnlyAnnoyingAlertChanged = false;
				HandlingEditorType = EditorType.CANCEL;
			}
		});
	}

	private static string ShowUpdateDateChoiceDialogFun(List<string> editotTypes)
	{
		if (HandlingOnlyAnnoyingAlertChanged && editotTypes.Contains(EditorType.ALL.ToString()))
		{
			HandlingEditorType = EditorType.ALL;
			TaskDetailPopup.TryCloseWindow();
			return EditorType.ALL.ToString();
		}
		TaskBaseViewModel taskById = TaskCache.GetTaskById(HandlingTaskId);
		ModifyRepeatDialog modifyRepeatDialog = new ModifyRepeatDialog(editotTypes, (!BatchHandle) ? null : taskById?.Title);
		DialogShowing = true;
		TaskDetailPopup.SetCanClose(value: false);
		modifyRepeatDialog.ShowDialog();
		TaskDetailPopup.SetCanClose(value: true);
		DialogShowing = false;
		EditorType editorType = (HandlingEditorType = modifyRepeatDialog.Selected);
		if (editorType != EditorType.CANCEL)
		{
			TaskDetailPopup.TryCloseWindow();
			if (HandlingFirstRecurrence)
			{
				if (taskById != null && taskById.RepeatFlag != null && !taskById.RepeatFlag.Contains("RRULE"))
				{
					CalendarConvertType repeatFlagType = CalendarConverter.GetRepeatFlagType(taskById.RepeatFlag);
					if (repeatFlagType != CalendarConvertType.Unknown)
					{
						editorType = EditorType.CANCEL;
						UpdateSubCalendarRepeatStart(repeatFlagType, modifyRepeatDialog.Selected, HandlingTaskId, HandleToTime);
					}
				}
				if (taskById != null && taskById.RepeatFlag != null && taskById.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY"))
				{
					editorType = EditorType.CANCEL;
					UpdateHolidayRepeat(modifyRepeatDialog.Selected, HandlingTaskId, HandleToTime);
				}
			}
			else if (taskById != null && taskById.RepeatFlag != null)
			{
				if (taskById.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY") && editorType == EditorType.FROM_CURRENT)
				{
					editorType = EditorType.CANCEL;
					UpdateHolidayRepeat(modifyRepeatDialog.Selected, HandlingTaskId, HandleToTime);
				}
				else if (taskById.RepeatFlag.Contains("FREQ=WEEKLY") && taskById.RepeatFlag.Contains("COUNT=") && editorType == EditorType.FROM_CURRENT)
				{
					RecurrenceModel recurrenceModel = RecurrenceModel.GetRecurrenceModel(taskById.RepeatFlag);
					if (recurrenceModel.Count > 0 && recurrenceModel.ByDay.Count == 2 && recurrenceModel.ByDay.Any((WeekDay x) => x.DayOfWeek == DayOfWeek.Sunday) && recurrenceModel.ByDay.Any((WeekDay x) => x.DayOfWeek == DayOfWeek.Saturday))
					{
						editorType = EditorType.CANCEL;
						UpdateCountRepeat(modifyRepeatDialog.Selected, HandlingTaskId, HandleToTime);
					}
				}
			}
		}
		return editorType.ToString();
	}

	public static bool ShouldApplyHandlingAnnoyingAlert(string taskId, bool isNewTask)
	{
		if (!HandlingAnnoyingAlertChanged || !HandlingAnnoyingAlert.HasValue)
		{
			return false;
		}
		if (HandlingEditorType == EditorType.ALL && taskId == HandlingTaskId)
		{
			return true;
		}
		if (isNewTask)
		{
			if (HandlingEditorType != EditorType.CURRENT)
			{
				return HandlingEditorType == EditorType.FROM_CURRENT;
			}
			return true;
		}
		return false;
	}

	private static async Task UpdateCountRepeat(EditorType type, string taskId, TimeData handleToTime)
	{
		TaskModel copy = await TaskService.CopyTask(taskId, _toastWindow as DependencyObject);
		System.DateTime? rawStartDate = copy.startDate;
		await TaskService.SetDate(copy.id, handleToTime, handleReminder: false, null, notify: false);
		await TaskService.UpdateNewFlagCountFromDateIndex(copy.id, rawStartDate, _recurringStart?.AddDays(-1.0), handleToTime, isHolidayWeekend: false);
		await TaskService.SetUntilToFlag(taskId, _recurringStart?.AddDays(-1.0), handleToTime, isHolidayWeekend: false);
	}

	private static async Task UpdateHolidayRepeat(EditorType type, string taskId, TimeData handleToTime)
	{
		switch (type)
		{
		case EditorType.CURRENT:
		{
			TaskModel obj = await TaskService.CopyTask(taskId, _toastWindow as DependencyObject);
			handleToTime.RepeatFlag = null;
			handleToTime.RepeatFrom = null;
			await TaskService.SetDate(obj.id, handleToTime);
			await TaskService.SkipCurrentRecurrence(taskId, notify: true, null, toast: false, handleCount: true);
			break;
		}
		case EditorType.ALL:
			if (handleToTime.StartDate.HasValue)
			{
				handleToTime.RepeatFlag = "RRULE:FREQ=DAILY;INTERVAL=1;TT_SKIP=OFFICIAL_WORKDAY";
			}
			await TaskService.SetDate(taskId, handleToTime);
			break;
		case EditorType.FROM_CURRENT:
		{
			TaskModel copy = await TaskService.CopyTask(taskId, _toastWindow as DependencyObject);
			System.DateTime? rawStartDate = copy.startDate;
			await TaskService.SetDate(copy.id, handleToTime, handleReminder: false, null, notify: false);
			await TaskService.UpdateNewFlagCountFromDateIndex(copy.id, rawStartDate, _recurringStart?.AddDays(-1.0), handleToTime, isHolidayWeekend: true);
			await TaskService.SetUntilToFlag(taskId, _recurringStart?.AddDays(-1.0), handleToTime, isHolidayWeekend: true);
			break;
		}
		}
	}

	private static async Task UpdateSubCalendarRepeatStart(CalendarConvertType calendarConvertType, EditorType type, string taskId, TimeData handleToTime)
	{
		switch (type)
		{
		case EditorType.CURRENT:
		{
			TaskModel obj = await TaskService.CopyTask(taskId, _toastWindow as DependencyObject);
			handleToTime.RepeatFlag = null;
			handleToTime.RepeatFrom = null;
			await TaskService.SetDate(obj.id, handleToTime);
			await TaskService.SkipCurrentRecurrence(taskId, notify: true, null, toast: false, handleCount: true);
			break;
		}
		case EditorType.ALL:
			if (handleToTime.StartDate.HasValue)
			{
				handleToTime.RepeatFlag = RepeatUtils.GetSubCalendarRepeatFlag(calendarConvertType, isMonthly: true, handleToTime.StartDate.Value);
			}
			await TaskService.SetDate(taskId, handleToTime);
			break;
		}
	}

	private static string ShowDeleteChoiceDialogFun(List<string> editotTypes)
	{
		TaskBaseViewModel taskById = TaskCache.GetTaskById(HandlingTaskId);
		EditorType editorType;
		if (BatchHandle && _batchDeleteTypes.ContainsKey(HandlingTaskId))
		{
			editorType = _batchDeleteTypes[HandlingTaskId];
		}
		else
		{
			ModifyRepeatDialog modifyRepeatDialog = new ModifyRepeatDialog(editotTypes, (!BatchHandle) ? null : taskById?.Title, isDelete: true);
			DialogShowing = true;
			TaskDetailPopup.SetCanClose(value: false);
			modifyRepeatDialog.ShowDialog();
			TaskDetailPopup.SetCanClose(value: true);
			DialogShowing = false;
			editorType = modifyRepeatDialog.Selected;
			if (BatchHandle)
			{
				_batchDeleteTypes[HandlingTaskId] = editorType;
				editorType = EditorType.CANCEL;
			}
		}
		if (editorType != EditorType.CANCEL)
		{
			TaskDetailPopup.TryCloseWindow();
		}
		if (editorType != EditorType.CANCEL)
		{
			TaskDetailPopup.TryCloseWindow();
			if (HandlingFirstRecurrence)
			{
				if (taskById != null && taskById.RepeatFlag != null && !taskById.RepeatFlag.Contains("RRULE") && CalendarConverter.GetRepeatFlagType(taskById.RepeatFlag) != CalendarConvertType.Unknown)
				{
					editorType = EditorType.CANCEL;
					DeleteFirstCurrent(HandlingTaskId, _toastWindow);
				}
			}
			else if (taskById != null && taskById.RepeatFlag != null && taskById.RepeatFlag.Contains("TT_SKIP=OFFICIAL_WORKDAY") && editorType == EditorType.FROM_CURRENT)
			{
				editorType = EditorType.CANCEL;
				UndoAbleDeleteHolidayWeekend(HandlingTaskId, _recurringStart?.AddDays(-1.0));
			}
		}
		return editorType.ToString();
	}

	private static async Task UndoAbleDeleteHolidayWeekend(string taskId, System.DateTime? targetDate)
	{
		List<TaskDeleteRecurrenceUndoEntity> undoModels = await GetHolidayWeekendDeleteUndoModel(taskId);
		TaskService.SetUntilToFlag(taskId, targetDate, null, isHolidayWeekend: true);
		_toastWindow?.ToastDeleteRecUndo(undoModels);
	}

	private static async Task<List<TaskDeleteRecurrenceUndoEntity>> GetHolidayWeekendDeleteUndoModel(string taskId)
	{
		List<TaskDeleteRecurrenceUndoEntity> undoModels = new List<TaskDeleteRecurrenceUndoEntity>();
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel == null)
		{
			return undoModels;
		}
		undoModels.Add(new TaskDeleteRecurrenceUndoEntity(new TaskDeleteUndoUpdateEntity
		{
			id = taskModel._Id,
			repeatFlag = taskModel.repeatFlag,
			sid = taskModel.id,
			startDate = ModelsDao.NewTTCalendarNullable(taskModel.startDate, taskModel.timeZone, handleTimezone: true),
			dueDate = ModelsDao.NewTTCalendarNullable(taskModel.dueDate, taskModel.timeZone, handleTimezone: true),
			taskStatus = taskModel.status,
			repeatFirstDate = ModelsDao.NewTTCalendarNullable(taskModel.repeatFirstDate, taskModel.timeZone, handleTimezone: true),
			snoozeRemindTime = ModelsDao.NewTTCalendarNullable(taskModel.remindTime, taskModel.timeZone, handleTimezone: true)
		}));
		return undoModels;
	}

	private static async void DeleteFirstCurrent(string taskId, IToastShowWindow toastWindow)
	{
		TaskModel taskModel = await TaskDao.GetThinTaskById(taskId);
		if (taskModel != null && taskModel.startDate.HasValue)
		{
			int addForgotCycle = 0;
			if (RepeatUtils.GetNextRepeatDate(taskModel, ref addForgotCycle).Date <= taskModel.startDate.Value.Date)
			{
				toastWindow?.TaskDeleted(taskId);
			}
			else
			{
				await TaskService.SkipCurrentRecurrence(taskId, notify: true, null, toast: false, handleCount: true);
			}
		}
	}

	private static async void DeleteTask(string taskId)
	{
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(task.attendId))
		{
			List<TaskModel> list = await TaskService.GetAllSubTasksByIdAsync(task.id, task.projectId);
			if (list != null && list.Count > 0)
			{
				list.Add(task);
				if (_toastWindow != null)
				{
					_toastWindow?.BatchDeleteTask(list);
				}
				else
				{
					TaskService.BatchDeleteTasks(list, showUndo: false);
				}
			}
			else if (_toastWindow != null)
			{
				_toastWindow?.TaskDeleted(task.id);
			}
			else
			{
				TaskService.DeleteTask(task.id);
			}
		}
		else if (await TaskOperationHelper.CheckIfAllowDeleteAgenda(task, null))
		{
			await TaskService.DeleteAgenda(task.id, task.projectId, task.attendId);
			SyncManager.TryDelaySync();
		}
	}

	private static void DeletePomodoroSummariesFun(string id)
	{
		PomoSummaryDao.DeletePomosByTaskId(id);
	}

	public static async Task<bool> TryUpdateDueDateOnlyDate(string taskId, System.DateTime? recurringStart, System.DateTime? recurringEnd, TimeData timeData, System.DateTime date, int fromCalendar, int fromDetail, bool batchHandle = false)
	{
		int num = 0;
		if (timeData.StartDate.HasValue)
		{
			if (timeData.DueDate.HasValue)
			{
				num = (int)(timeData.DueDate.Value - timeData.StartDate.Value).TotalMinutes;
			}
			timeData.StartDate = DateUtils.SetDateOnly(timeData.StartDate.Value, date);
			if (num > 0)
			{
				timeData.DueDate = timeData.StartDate.Value.AddMinutes(num);
			}
		}
		else
		{
			timeData.StartDate = date;
			timeData.DueDate = null;
			timeData.IsAllDay = true;
		}
		return await TryUpdateDueDate(taskId, recurringStart, recurringEnd, timeData, fromCalendar, fromDetail, batchHandle);
	}

	public static async Task<bool> TryUpdateDueDate(string taskId, System.DateTime? recurringStart, System.DateTime? recurringEnd, TimeData reviseData, int fromCalendar, int fromDetail, bool batchHandle = false)
	{
		if (!_inited || reviseData == null)
		{
			return false;
		}
		TaskModel task = await TaskDao.GetTaskById(taskId);
		if (task == null)
		{
			return false;
		}
		while (DialogShowing)
		{
			await Task.Delay(30);
		}
		HandlingTaskId = taskId;
		_handlingVersion++;
		BatchHandle = batchHandle;
		HandleToTime = reviseData;
		HandlingAnnoyingAlert = reviseData.AnnoyingAlert;
		HandlingAnnoyingAlertChanged = reviseData.AnnoyingAlert.HasValue && task.annoyingAlert != reviseData.AnnoyingAlert;
		HandlingOnlyAnnoyingAlertChanged = false;
		HandlingEditorType = EditorType.CANCEL;
		HandlingFirstRecurrence = recurringStart == task.startDate;
		_recurringStart = recurringStart;
		bool isRepeatChanged = task.repeatFlag != reviseData.RepeatFlag || task.repeatFrom != reviseData.RepeatFrom;
		List<TaskReminderModel> reminders = await TaskReminderDao.GetRemindersByTaskId(task.id);
		bool isReminderChanged = !TaskReminderDao.IsEquals(reminders, reviseData.Reminders);
		if (recurringStart != reviseData.StartDate || recurringEnd != reviseData.DueDate || (task.isAllDay ?? true) != (reviseData.IsAllDay ?? true) || task.timeZone != reviseData.TimeZone.TimeZoneName || task.Floating != reviseData.TimeZone.IsFloat || isRepeatChanged || isReminderChanged || HandlingAnnoyingAlertChanged)
		{
			HandlingOnlyAnnoyingAlertChanged = HandlingAnnoyingAlertChanged && recurringStart == reviseData.StartDate && recurringEnd == reviseData.DueDate && (task.isAllDay ?? true) == (reviseData.IsAllDay ?? true) && task.timeZone == reviseData.TimeZone.TimeZoneName && task.Floating == reviseData.TimeZone.IsFloat && !isRepeatChanged && !isReminderChanged;
			if (HandlingOnlyAnnoyingAlertChanged)
			{
				await ApplyAnnoyingAlertToAllRecurrences(task);
				return true;
			}
			List<TaskDetailItemModel> items = await TaskDetailItemDao.GetCheckItemsByTaskId(task.id);
			DueDataResult duedataSetModel = new DueDataResult
			{
				origin = ModelsDao.NewDueDataModel(task, reminders, recurringStart, recurringEnd),
				revise = ModelsDao.NewDueDataModel(task, reviseData),
				isReminderChanged = ((isReminderChanged || HandlingAnnoyingAlertChanged) ? 1 : 0),
				onlyDateChanged = ((!(isReminderChanged || isRepeatChanged) && !HandlingAnnoyingAlertChanged) ? 1 : 0)
			};
			TASK task2 = TASKDao.NewTASK(task, items, reminders, recurringStart ?? task.startDate, recurringEnd ?? task.dueDate);
			try
			{
				ModifySingleRepeat.UpdateDueData(task2, duedataSetModel, fromCalendar, fromDetail, 0);
			}
			catch (Exception)
			{
				TaskChangeNotifier.NotifyTaskDateChanged(task.id);
			}
			return true;
		}
		return false;
	}

	private static async Task ApplyAnnoyingAlertToAllRecurrences(TaskModel task)
	{
		task.annoyingAlert = HandlingAnnoyingAlert;
		task.modifiedTime = System.DateTime.Now;
		await TaskService.UpdateTask(task, CheckMatchedType.CheckSmart);
		await SyncStatusDao.AddModifySyncStatus(task.id);
		TaskChangeNotifier.NotifyTaskDateChanged(task.id);
		GlobalEventManager.NotifyReloadCalendar();
		HandlingAnnoyingAlert = null;
		HandlingAnnoyingAlertChanged = false;
		HandlingOnlyAnnoyingAlertChanged = false;
		HandlingEditorType = EditorType.CANCEL;
	}

	public static async Task TryDeleteRecurrence(string taskId, System.DateTime? recurringStart, System.DateTime? recurringEnd, IToastShowWindow toastWindow, bool batchHandle = false, List<TaskDeleteRecurrenceUndoEntity> batchDeleteUndo = null)
	{
		if (!_inited)
		{
			return;
		}
		TaskModel task = await TaskDao.GetTaskById(taskId);
		if (task == null)
		{
			return;
		}
		while (DialogShowing)
		{
			await Task.Delay(30);
		}
		_toastWindow = toastWindow;
		BatchHandle = batchHandle;
		_batchDeleteUndoModels = batchDeleteUndo;
		HandlingTaskId = taskId;
		HandlingFirstRecurrence = recurringStart == task.startDate;
		_recurringStart = recurringStart;
		TASK item = TASKDao.NewTASK(task, null, null, recurringStart ?? task.startDate, recurringEnd ?? task.dueDate);
		List<TASK> tasks = new List<TASK> { item };
		try
		{
			ModifySingleRepeat.DeleteRecurrence(tasks, 1);
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show(ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public static async Task<bool> CompleteOrSkipRecurrence(string taskId, System.DateTime? dateTime, IToastShowWindow toastWindow = null)
	{
		if (!dateTime.HasValue)
		{
			return true;
		}
		TaskModel task = await TaskDao.GetThinTaskById(taskId);
		if (task == null || task.status != 0 || string.IsNullOrEmpty(task.repeatFlag) || !task.startDate.HasValue)
		{
			return true;
		}
		if (task.remindTime.HasValue)
		{
			task.remindTime = null;
			await ReminderDelayDao.DeleteByIdAsync(task.id, "task");
		}
		double totalDays = (dateTime.Value.Date - task.startDate.Value.Date).TotalDays;
		System.DateTime startDate = (task.ParseTz ? TimeZoneUtils.LocalToTargetTzTime(task.startDate.Value, task.timeZone) : task.startDate.Value);
		List<string> exDate = ((!string.IsNullOrEmpty(task.exDates)) ? ExDateSerilizer.ToArray(task.exDates).ToList() : null);
		List<System.DateTime> list = (from r in RepeatUtils.GetValidRepeatDates(task.repeatFlag, task.repeatFrom, startDate, startDate.Date, startDate.AddDays(totalDays).Date, exDate, task.timeZone, task.ParseTz)
			where r.Date != dateTime.Value.Date
			select r).ToList();
		if (list.All((System.DateTime d) => d.Date != task.startDate.Value.Date))
		{
			list.Insert(0, task.startDate.Value);
		}
		if (list.Count == 0)
		{
			return true;
		}
		ModifyRepeatDialog modifyRepeatDialog = new ModifyRepeatDialog(list);
		TaskDetailPopup.SetCanClose(value: false);
		modifyRepeatDialog.ShowDialog();
		TaskDetailPopup.SetCanClose(value: true);
		switch (modifyRepeatDialog.Selected)
		{
		case EditorType.COMPLETEALL:
			await CompleteAllTaskRecurrence(task, list, dateTime.Value);
			break;
		case EditorType.SKIP:
			await SkipAllTaskRecurrence(task, list, dateTime.Value, toastWindow);
			break;
		default:
			return false;
		}
		TaskChangeNotifier.NotifyBatchDateChanged(new List<string> { taskId });
		return true;
	}

	private static async Task SkipAllTaskRecurrence(TaskModel task, List<System.DateTime> dates, System.DateTime endTime, IToastShowWindow toastWindow)
	{
		if (dates == null || dates.Count == 0 || !task.startDate.HasValue)
		{
			return;
		}
		int num = 0;
		List<string> list = new List<string>();
		if (task.exDate != null)
		{
			list = task.exDate.ToList();
		}
		if (task.startDate.HasValue && list.Count > 0)
		{
			int result;
			List<int> source = list.Select((string ex) => int.TryParse(ex, out result) ? result : 0).ToList();
			int startDateNum = DateUtils.GetDateNum(task.startDate.Value);
			int nextDateNum = DateUtils.GetDateNum(endTime);
			num = source.Count((int num3) => num3 > startDateNum && num3 < nextDateNum && num3 != 0);
		}
		dates.Sort((System.DateTime a, System.DateTime b) => a.CompareTo(b));
		System.DateTime originalDate = task.startDate.Value;
		System.DateTime dateTime = dates.FirstOrDefault((System.DateTime d) => d.Date == endTime.Date);
		task.startDate = ((!Utils.IsEmptyDate(dateTime)) ? dateTime : DateUtils.SetHourAndMinuteOnly(endTime, originalDate.Hour, originalDate.Minute));
		if (task.dueDate.HasValue)
		{
			task.dueDate = task.startDate.Value.AddMinutes((task.dueDate.Value - originalDate).TotalMinutes);
		}
		if (!string.IsNullOrEmpty(task.repeatFlag))
		{
			int num2 = dates.Count((System.DateTime d) => d.Date != endTime.Date);
			if (task.repeatFlag.Contains("FORGETTINGCURVE"))
			{
				task.repeatFlag = RepeatUtils.GetNextEbbinghausCycle(task.repeatFlag, num2 + num);
			}
			if (task.repeatFlag.Contains("COUNT"))
			{
				(toastWindow ?? App.Window).TryToastString(null, Utils.GetString("SkipCountToast"));
			}
		}
		List<TaskDetailItemModel> list2 = await TaskDetailItemDao.GetCheckItemsByTaskId(task.id);
		if (list2 != null && list2.Any())
		{
			double totalMinutes = (task.startDate.Value - originalDate).TotalMinutes;
			List<TaskDetailItemModel> list3 = new List<TaskDetailItemModel>();
			foreach (TaskDetailItemModel item in list2)
			{
				bool flag = false;
				if (item.status != 0)
				{
					item.status = 0;
					flag = true;
				}
				if (item.startDate.HasValue)
				{
					item.startDate = item.startDate.Value.AddMinutes(totalMinutes);
					flag = true;
				}
				if (flag)
				{
					list3.Add(item);
				}
			}
			if (list3.Any())
			{
				await TaskDetailItemDao.BatchUpdateChecklists(list3);
			}
		}
		await TaskService.UpdateTaskOnTimeChanged(task);
		TaskChangeNotifier.NotifyTaskDateChanged(task.id);
		await SyncStatusDao.AddModifySyncStatus(task.id);
	}

	private static async Task CompleteAllTaskRecurrence(TaskModel originalTask, List<System.DateTime> dates, System.DateTime endTime)
	{
		if (dates == null || dates.Count == 0 || !originalTask.startDate.HasValue)
		{
			return;
		}
		int num = 0;
		List<string> list = new List<string>();
		if (originalTask.exDate != null)
		{
			list = originalTask.exDate.ToList();
		}
		if (originalTask.startDate.HasValue && list.Count > 0)
		{
			int result;
			List<int> source = list.Select((string ex) => int.TryParse(ex, out result) ? result : 0).ToList();
			int startDateNum = DateUtils.GetDateNum(originalTask.startDate.Value);
			int nextDateNum = DateUtils.GetDateNum(endTime);
			num = source.Count((int num4) => num4 > startDateNum && num4 < nextDateNum && num4 != 0);
		}
		List<TaskModel> repeatTasks = new List<TaskModel>();
		long num2 = (ProjectSortOrderDao.GetNextTaskSortOrderInProject(originalTask.projectId, originalTask.sortOrder, originalTask.parentId) - originalTask.sortOrder) / dates.Count;
		dates.Sort((System.DateTime a, System.DateTime b) => a.CompareTo(b));
		double value = 0.0;
		System.DateTime value2 = originalTask.startDate.Value;
		System.DateTime dateTime = dates.FirstOrDefault((System.DateTime d) => d.Date == endTime.Date);
		originalTask.startDate = ((!Utils.IsEmptyDate(dateTime)) ? dateTime : DateUtils.SetHourAndMinuteOnly(endTime, value2.Hour, value2.Minute));
		if (originalTask.dueDate.HasValue)
		{
			value = (originalTask.dueDate.Value - value2).TotalMinutes;
			originalTask.dueDate = originalTask.startDate.Value.AddMinutes(value);
		}
		for (int num3 = 0; num3 < dates.Count; num3++)
		{
			System.DateTime value3 = dates[num3];
			if (!(value3.Date == endTime.Date))
			{
				TaskModel item = new TaskModel(originalTask)
				{
					id = Utils.GetGuid(),
					status = 2,
					repeatFlag = string.Empty,
					completedTime = System.DateTime.Now,
					repeatTaskId = originalTask.id,
					sortOrder = originalTask.sortOrder + num3 * num2,
					completedUserId = Utils.GetCurrentUserIdInt().ToString(),
					startDate = value3,
					dueDate = ((!originalTask.dueDate.HasValue) ? ((System.DateTime?)null) : new System.DateTime?(value3.AddMinutes(value)))
				};
				repeatTasks.Add(item);
			}
		}
		if (!string.IsNullOrEmpty(originalTask.repeatFlag))
		{
			int repeatCount = RepeatUtils.GetRepeatCount(originalTask.repeatFlag);
			repeatCount -= repeatTasks.Count;
			if (originalTask.repeatFlag.Contains("FORGETTINGCURVE"))
			{
				originalTask.repeatFlag = RepeatUtils.GetNextEbbinghausCycle(originalTask.repeatFlag, repeatTasks.Count + num);
			}
			if (repeatCount > 0)
			{
				originalTask.repeatFlag = RepeatUtils.GetRepeatFlag(originalTask.repeatFlag, default(System.DateTime), repeatCount);
			}
		}
		if (originalTask.remindTime.HasValue)
		{
			originalTask.remindTime = null;
			await ReminderDelayDao.DeleteByIdAsync(originalTask.id, "task");
		}
		originalTask.status = 0;
		originalTask.progress = null;
		for (int i = 0; i < repeatTasks.Count; i++)
		{
			TaskModel repeatTask = (repeatTasks[i] = await TaskDao.InsertTask(repeatTasks[i]));
			SyncStatusDao.AddSyncStatus(repeatTasks[i].id, 4);
			if (!string.IsNullOrEmpty(repeatTask.parentId))
			{
				await SyncStatusDao.AddSetParentSyncStatus(repeatTask.id, "");
			}
			await TaskService.TryCloseAndCopySubTask(originalTask, repeatTask, needUndo: false, 2);
			await ProjectCopyManager.CopyChecklistItems(originalTask.id, repeatTask.id);
			List<TaskReminderModel> list2 = await TaskReminderDao.GetRemindersByTaskId(originalTask.id);
			TaskModel taskModel2 = repeatTask;
			taskModel2.reminders = await Utils.CopyReminderItem(repeatTask, list2.ToArray());
			taskModel2 = repeatTask;
			taskModel2.Attachments = await Utils.CopyAttachmentItem(repeatTask, originalTask.id);
			if (i == 0)
			{
				taskModel2 = repeatTask;
				taskModel2.FocusSummaries = await Utils.CopyPomoSummaries(repeatTask, originalTask.id);
				await PomoSummaryDao.CleanPomosByTaskId(originalTask.id);
				TaskService.ClearMovedPomoSummaries(originalTask);
				await TickFocusManager.NotifyPomoTaskChanged(originalTask.id, repeatTask.id);
			}
			await TaskDao.UpdateTask(repeatTask);
		}
		if (!string.IsNullOrEmpty(originalTask.parentId))
		{
			await TaskDao.AddOrRemoveTaskChildIds(originalTask.parentId, repeatTasks.Select((TaskModel t) => t.id).ToList(), addChild: true);
		}
		if (originalTask.kind == "CHECKLIST")
		{
			TaskModel repeatTask = originalTask;
			repeatTask.progress = await TaskService.CalculateProgress(originalTask.id);
		}
		await TaskService.UpdateTask(originalTask);
		await SyncStatusDao.AddModifySyncStatus(originalTask.id);
	}

	public static async void DelayCollect(object obj)
	{
		await Task.Delay(5000);
		_ = obj;
	}

	public static void ClearBatchDelete()
	{
		_batchDeleteTypes.Clear();
		_batchDeleteUndoModels = null;
	}

	public static List<string> GetBatchDeleteAllIds()
	{
		List<string> list = new List<string>();
		foreach (string item in _batchDeleteTypes.Keys.ToList())
		{
			if (_batchDeleteTypes.TryGetValue(item, out var value) && value == EditorType.ALL)
			{
				_batchDeleteTypes.TryRemove(item, out var _);
				list.Add(item);
			}
		}
		return list;
	}
}
