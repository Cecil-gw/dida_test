using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

public class NewTaskKindConvertCache
{
	private DateTime? _startDate;

	private DateTime? _dueDate;

	private bool? _isAllDay;

	private DateTime? _remindTime;

	private bool _isFloating;

	private string _timeZoneName;

	private int _priority;

	private int _status;

	private DateTime? _completedTime;

	private int? _progress;

	private string _repeatFlag;

	private string _repeatFrom;

	private List<string> _exDates;

	private TaskReminderModel[] _reminders;

	private int? _annoyingAlert;

	private AttachmentModel[] _attachments;

	private string _calendarId;

	private string _location;

	private TimeData _calendarTimeData;

	private bool _repeatHiddenForNote;

	private List<string> _attachmentMarkdown;

	private string _contentBeforeCalendar;

	private string _calendarContentAtConvert;

	public BindCalendarModel Calendar
	{
		get
		{
			if (string.IsNullOrEmpty(_calendarId))
			{
				return null;
			}
			BindCalendarModel calendar = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel item) => item != null && item.Id == _calendarId && item.Accessible);
			if (calendar == null)
			{
				return null;
			}
			BindCalendarAccountModel bindCalendarAccountModel = CacheManager.GetBindCalendarAccounts().FirstOrDefault((BindCalendarAccountModel item) => item != null && item.Id == calendar.AccountId);
			if (bindCalendarAccountModel != null && !bindCalendarAccountModel.Expired)
			{
				return calendar;
			}
			return null;
		}
	}

	public string Location => _location;

	public TimeData CalendarTimeData
	{
		get
		{
			if (_calendarTimeData != null)
			{
				return TimeData.Clone(_calendarTimeData);
			}
			return null;
		}
	}

	public static NewTaskKindConvertCache From(TaskDetailViewModel model)
	{
		return new NewTaskKindConvertCache
		{
			_startDate = model?.SourceViewModel?.StartDate,
			_dueDate = model?.SourceViewModel?.DueDate,
			_isAllDay = model?.SourceViewModel?.IsAllDay,
			_remindTime = model?.SourceViewModel?.RemindTime,
			_isFloating = (model?.SourceViewModel?.IsFloating == true),
			_timeZoneName = model?.SourceViewModel?.TimeZoneName,
			_priority = (model?.SourceViewModel?.Priority).GetValueOrDefault(),
			_status = (model?.SourceViewModel?.Status).GetValueOrDefault(),
			_completedTime = model?.SourceViewModel?.CompletedTime,
			_progress = model?.SourceViewModel?.Progress,
			_repeatFlag = model?.SourceViewModel?.RepeatFlag,
			_repeatFrom = model?.SourceViewModel?.RepeatFrom,
			_exDates = model?.ExDates?.ToList(),
			_reminders = CopyReminders(model?.Reminders),
			_annoyingAlert = model?.AnnoyingAlert,
			_attachments = CopyAttachments(model?.Attachments, model?.TaskId),
			_attachmentMarkdown = TaskUtils.GetAttachmentMarkdown(model?.TaskContent),
			_calendarId = model?.SourceViewModel?.CalendarId
		};
	}

	public NewTaskKindConvertCache Copy()
	{
		return new NewTaskKindConvertCache
		{
			_startDate = _startDate,
			_dueDate = _dueDate,
			_isAllDay = _isAllDay,
			_remindTime = _remindTime,
			_isFloating = _isFloating,
			_timeZoneName = _timeZoneName,
			_priority = _priority,
			_status = _status,
			_completedTime = _completedTime,
			_progress = _progress,
			_repeatFlag = _repeatFlag,
			_repeatFrom = _repeatFrom,
			_exDates = _exDates?.ToList(),
			_reminders = CopyReminders(_reminders),
			_annoyingAlert = _annoyingAlert,
			_attachments = CopyAttachments(_attachments),
			_attachmentMarkdown = _attachmentMarkdown?.ToList(),
			_contentBeforeCalendar = _contentBeforeCalendar,
			_calendarContentAtConvert = _calendarContentAtConvert,
			_calendarId = _calendarId,
			_location = _location,
			_calendarTimeData = ((_calendarTimeData == null) ? null : TimeData.Clone(_calendarTimeData)),
			_repeatHiddenForNote = _repeatHiddenForNote
		};
	}

	public void CacheRepeatHiddenForNote(TimeData timeData)
	{
		if (timeData != null && !string.IsNullOrWhiteSpace(timeData.RepeatFlag))
		{
			_repeatFlag = timeData.RepeatFlag;
			_repeatFrom = timeData.RepeatFrom;
			_exDates = timeData.ExDates?.ToList();
			_repeatHiddenForNote = true;
		}
	}

	public void UpdateAttachments(string content, IEnumerable<AttachmentModel> attachments, string taskId)
	{
		_attachmentMarkdown = TaskUtils.GetAttachmentMarkdown(content);
		_attachments = CopyAttachments(attachments, taskId);
	}

	public void CacheAttachmentContentForCalendar(string originalContent, string calendarContent)
	{
		_attachmentMarkdown = TaskUtils.GetAttachmentMarkdown(originalContent);
		_contentBeforeCalendar = originalContent ?? string.Empty;
		_calendarContentAtConvert = calendarContent ?? string.Empty;
	}

	public string RestoreAttachmentMarkdown(string content)
	{
		return TaskUtils.RestoreAttachmentMarkdown(content, _attachmentMarkdown, _contentBeforeCalendar, _calendarContentAtConvert);
	}

	public void ApplyStandardRepeatTo(TimeData timeData)
	{
		if (_repeatHiddenForNote && timeData != null && string.IsNullOrWhiteSpace(timeData.RepeatFlag) && RepeatUtils.IsStandardRRule(_repeatFlag))
		{
			timeData.RepeatFlag = _repeatFlag;
			timeData.RepeatFrom = _repeatFrom;
			timeData.ExDates = _exDates?.ToList();
			_repeatHiddenForNote = false;
		}
	}

	public void SetCalendarFields(string calendarId, string location, TimeData calendarTimeData = null)
	{
		_calendarId = calendarId;
		_location = location;
		if (calendarTimeData != null)
		{
			_calendarTimeData = TimeData.Clone(calendarTimeData);
		}
	}

	public TimeData MergeCalendarTimeForEvent(TimeData currentTaskTimeData)
	{
		TimeData calendarTimeData = CalendarTimeData;
		if (calendarTimeData == null || (currentTaskTimeData != null && currentTaskTimeData.DueDate.HasValue) || !Nullable.Equals(currentTaskTimeData?.StartDate, calendarTimeData.StartDate))
		{
			return currentTaskTimeData;
		}
		calendarTimeData.RepeatFlag = currentTaskTimeData.RepeatFlag;
		calendarTimeData.RepeatFrom = currentTaskTimeData.RepeatFrom;
		calendarTimeData.ExDates = currentTaskTimeData.ExDates?.ToList();
		return calendarTimeData;
	}

	public void KeepCalendarFieldsFrom(NewTaskKindConvertCache other)
	{
		if (other != null)
		{
			if (string.IsNullOrEmpty(_calendarId))
			{
				_calendarId = other._calendarId;
			}
			if (_location == null)
			{
				_location = other._location;
			}
			if (_calendarTimeData == null)
			{
				_calendarTimeData = ((other._calendarTimeData == null) ? null : TimeData.Clone(other._calendarTimeData));
			}
		}
	}

	public void ApplyTo(TaskDetailViewModel model)
	{
		if (model?.SourceViewModel != null)
		{
			TaskBaseViewModel sourceViewModel = model.SourceViewModel;
			bool repeatHiddenForNote = _repeatHiddenForNote;
			sourceViewModel.Priority = _priority;
			sourceViewModel.Status = _status;
			sourceViewModel.CompletedTime = _completedTime;
			if (_progress.HasValue)
			{
				sourceViewModel.Progress = _progress.Value;
			}
			if (string.IsNullOrEmpty(sourceViewModel.RepeatFlag))
			{
				sourceViewModel.RepeatFlag = _repeatFlag;
				sourceViewModel.RepeatFrom = _repeatFrom;
			}
			TimeData calendarTimeData = CalendarTimeData;
			if (calendarTimeData != null && CanRestoreTime(sourceViewModel.StartDate, calendarTimeData.StartDate))
			{
				ApplyTimeData(model, sourceViewModel, calendarTimeData);
			}
			else if (CanRestoreTime(sourceViewModel.StartDate, _startDate))
			{
				sourceViewModel.StartDate = _startDate;
				sourceViewModel.DueDate = _dueDate;
				sourceViewModel.IsAllDay = _isAllDay;
				sourceViewModel.RemindTime = _remindTime;
				sourceViewModel.IsFloating = _isFloating;
				sourceViewModel.TimeZoneName = _timeZoneName;
				model.Reminders = CopyReminders(_reminders);
				model.AnnoyingAlert = _annoyingAlert;
				model.ExDates = _exDates?.ToList();
			}
			if ((repeatHiddenForNote && string.IsNullOrWhiteSpace(sourceViewModel.RepeatFlag)) || HasCalendarUnsupportedRepeat())
			{
				sourceViewModel.RepeatFlag = _repeatFlag;
				sourceViewModel.RepeatFrom = _repeatFrom;
				model.ExDates = _exDates?.ToList();
			}
			if (repeatHiddenForNote && model.ParseData != null && string.IsNullOrWhiteSpace(model.ParseData.RepeatFlag))
			{
				model.ParseData.RepeatFlag = sourceViewModel.RepeatFlag;
				model.ParseData.RepeatFrom = sourceViewModel.RepeatFrom;
				model.ParseData.ExDates = model.ExDates?.ToList();
			}
			_repeatHiddenForNote = false;
			ApplyAttachmentsTo(model);
			model.SetPropertyChanged("Priority");
			model.SetPropertyChanged("Status");
			model.SetPropertyChanged("Progress");
			model.SetPropertyChanged("Kind");
			model.SetPropertyChanged("DisplayStartDate");
			model.SetPropertyChanged("DisplayDueDate");
			model.SetPropertyChanged("IsAllDay");
			model.SetPropertyChanged("DateText");
			model.SetPropertyChanged("RepeatText");
			model.SetPropertyChanged("ShowRepeatIcon");
			model.SetPropertyChanged("TimeZoneText");
		}
	}

	public void ApplyAttachmentsTo(TaskDetailViewModel model)
	{
		if (model != null)
		{
			model.Attachments = MergeAttachments(_attachments, model.Attachments, model.TaskId);
		}
	}

	private bool HasCalendarUnsupportedRepeat()
	{
		if (!string.IsNullOrWhiteSpace(_repeatFlag))
		{
			return !RepeatUtils.IsStandardRRule(_repeatFlag);
		}
		return false;
	}

	private static void ApplyTimeData(TaskDetailViewModel model, TaskBaseViewModel source, TimeData timeData)
	{
		source.StartDate = timeData.StartDate;
		source.DueDate = timeData.DueDate;
		source.IsAllDay = timeData.IsAllDay;
		source.RemindTime = null;
		source.IsFloating = timeData.TimeZone?.IsFloat ?? false;
		source.TimeZoneName = timeData.TimeZone?.TimeZoneName;
		source.RepeatFlag = timeData.RepeatFlag;
		source.RepeatFrom = timeData.RepeatFrom;
		model.Reminders = CopyReminders(timeData.Reminders);
		model.AnnoyingAlert = timeData.AnnoyingAlert;
		model.ExDates = timeData.ExDates?.ToList();
	}

	private bool CanRestoreTime(DateTime? currentStartDate, DateTime? cachedStartDate)
	{
		if (currentStartDate.HasValue)
		{
			return Nullable.Equals(currentStartDate, cachedStartDate);
		}
		return true;
	}

	private static TaskReminderModel[] CopyReminders(IEnumerable<TaskReminderModel> reminders)
	{
		return reminders?.Select((TaskReminderModel reminder) => new TaskReminderModel
		{
			id = reminder.id,
			Taskid = reminder.Taskid,
			taskserverid = reminder.taskserverid,
			trigger = reminder.trigger
		}).ToArray();
	}

	private static AttachmentModel[] CopyAttachments(IEnumerable<AttachmentModel> attachments, string taskId = null)
	{
		return attachments?.Where((AttachmentModel attachment) => attachment != null).Select(delegate(AttachmentModel attachment)
		{
			AttachmentModel attachmentModel = AttachmentModel.Copy(attachment);
			if (!string.IsNullOrEmpty(taskId))
			{
				attachmentModel.taskId = taskId;
			}
			return attachmentModel;
		}).ToArray();
	}

	private static AttachmentModel[] MergeAttachments(IEnumerable<AttachmentModel> cached, IEnumerable<AttachmentModel> current, string taskId)
	{
		List<AttachmentModel> list = new List<AttachmentModel>();
		AddAttachments(list, cached, taskId);
		AddAttachments(list, current, taskId);
		if (list.Count != 0)
		{
			return list.ToArray();
		}
		return null;
	}

	private static void AddAttachments(ICollection<AttachmentModel> target, IEnumerable<AttachmentModel> attachments, string taskId)
	{
		if (attachments == null)
		{
			return;
		}
		foreach (AttachmentModel attachment in attachments)
		{
			if (attachment != null && !target.Any((AttachmentModel item) => SameAttachment(item, attachment)))
			{
				AttachmentModel attachmentModel = AttachmentModel.Copy(attachment);
				if (!string.IsNullOrEmpty(taskId))
				{
					attachmentModel.taskId = taskId;
				}
				target.Add(attachmentModel);
			}
		}
	}

	private static bool SameAttachment(AttachmentModel left, AttachmentModel right)
	{
		if (!SameNonEmpty(left.id, right.id) && !SameNonEmpty(left.refId, right.refId) && !SameNonEmpty(left.localPath, right.localPath))
		{
			return SameNonEmpty(left.path, right.path);
		}
		return true;
	}

	private static bool SameNonEmpty(string left, string right)
	{
		if (!string.IsNullOrEmpty(left))
		{
			return left == right;
		}
		return false;
	}
}
