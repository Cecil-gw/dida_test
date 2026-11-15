using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class AddTaskKindConvertCache
{
	private int _priority;

	private bool _manuallySelectedPriority;

	private TimeData _timeData;

	private List<string> _filePaths;

	private List<string> _tags;

	private string _calendarId;

	private string _calendarName;

	private string _accountId;

	private string _projectId;

	private string _projectName;

	private string _columnId;

	private string _addToColumnId;

	private string _projectTokenValue;

	private string _assignee;

	private string _location;

	private bool _repeatHiddenForNote;

	private List<string> _attachmentMarkdown;

	private string _contentBeforeCalendar;

	private string _calendarContentAtConvert;

	public string Location => _location;

	public string ProjectTokenValue => _projectTokenValue;

	public string ProjectId => _projectId;

	public string ColumnId => _columnId;

	public string AddToColumnId => _addToColumnId;

	public static AddTaskKindConvertCache From(AddTaskViewModel model)
	{
		return new AddTaskKindConvertCache
		{
			_priority = (model?.Priority ?? 0),
			_manuallySelectedPriority = (model?.ManuallySelectedPriority ?? false),
			_timeData = TimeData.Clone(model?.TimeData),
			_tags = ((from tag in model?.Tags?.Where((string tag) => !string.IsNullOrWhiteSpace(tag))
				select tag.Replace("#", string.Empty).ToLower()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>()),
			_calendarId = model?.CalendarId,
			_calendarName = model?.CalendarName,
			_accountId = model?.AccountId,
			_projectId = model?.ProjectId,
			_projectName = model?.ProjectName,
			_columnId = model?.ColumnId,
			_addToColumnId = model?.AddToColumnId,
			_assignee = model?.Assignee,
			_attachmentMarkdown = TaskUtils.GetAttachmentMarkdown(model?.ContentText),
			_filePaths = ((from file in model?.Files
				select file.Path into path
				where !string.IsNullOrEmpty(path)
				select path).Distinct().ToList() ?? new List<string>())
		};
	}

	public void ApplyTo(AddTaskViewModel model, bool includeTags = false, bool includeSharedFields = true)
	{
		if (model != null)
		{
			model.Priority = _priority;
			model.ManuallySelectedPriority = _manuallySelectedPriority;
			if (model.TimeData == null || !model.TimeData.StartDate.HasValue)
			{
				model.TimeData = TimeData.Clone(_timeData);
			}
			if (((_repeatHiddenForNote && string.IsNullOrWhiteSpace(model.TimeData?.RepeatFlag)) || HasCalendarUnsupportedRepeat()) && model.TimeData != null)
			{
				model.TimeData.RepeatFlag = _timeData.RepeatFlag;
				model.TimeData.RepeatFrom = _timeData.RepeatFrom;
				model.TimeData.ExDates = _timeData.ExDates?.ToList();
			}
			_repeatHiddenForNote = false;
			if (includeSharedFields)
			{
				ApplyFilesTo(model, includeTags);
			}
		}
	}

	private bool HasCalendarUnsupportedRepeat()
	{
		if (!string.IsNullOrWhiteSpace(_timeData?.RepeatFlag))
		{
			return !RepeatUtils.IsStandardRRule(_timeData.RepeatFlag);
		}
		return false;
	}

	public void CacheRepeatHiddenForNote(TimeData timeData)
	{
		if (timeData != null && !string.IsNullOrWhiteSpace(timeData.RepeatFlag))
		{
			if (_timeData == null)
			{
				_timeData = TimeData.Clone(timeData);
			}
			else
			{
				_timeData.RepeatFlag = timeData.RepeatFlag;
				_timeData.RepeatFrom = timeData.RepeatFrom;
				_timeData.ExDates = timeData.ExDates?.ToList();
			}
			_repeatHiddenForNote = true;
		}
	}

	public void ApplyStandardRepeatTo(TimeData timeData)
	{
		if (_repeatHiddenForNote && timeData != null && string.IsNullOrWhiteSpace(timeData.RepeatFlag) && RepeatUtils.IsStandardRRule(_timeData?.RepeatFlag))
		{
			timeData.RepeatFlag = _timeData.RepeatFlag;
			timeData.RepeatFrom = _timeData.RepeatFrom;
			timeData.ExDates = _timeData.ExDates?.ToList();
			_repeatHiddenForNote = false;
		}
	}

	public void ApplyFilesTo(AddTaskViewModel model, bool includeTags = false)
	{
		if (model != null)
		{
			ApplyProjectTo(model);
			model.Assignee = _assignee;
			model.MergeFiles(_filePaths);
			if (includeTags)
			{
				model.Tags = _tags?.ToList() ?? new List<string>();
				model.NotifyDisplayTagsChanged();
			}
		}
	}

	private void ApplyProjectTo(AddTaskViewModel model)
	{
		if (!string.IsNullOrEmpty(_projectId))
		{
			model.ProjectId = _projectId;
		}
		if (!string.IsNullOrEmpty(_projectName))
		{
			model.ProjectName = _projectName;
		}
		model.ColumnId = _columnId;
		model.AddToColumnId = _addToColumnId;
		model.NotifyProjectChanged();
	}

	public void SetProjectTokenValue(string projectTokenValue)
	{
		if (!string.IsNullOrEmpty(projectTokenValue))
		{
			_projectTokenValue = projectTokenValue;
		}
	}

	public void UpdateSharedTaskFields(AddTaskViewModel model, string projectTokenValue)
	{
		if (model == null || model.IsCalendar)
		{
			SetProjectTokenValue(projectTokenValue);
			return;
		}
		_tags = (from tag in model.Tags?.Where((string tag) => !string.IsNullOrWhiteSpace(tag))
			select tag.Replace("#", string.Empty).ToLower()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
		_projectId = model.ProjectId;
		_projectName = model.ProjectName;
		_columnId = model.ColumnId;
		_addToColumnId = model.AddToColumnId;
		_assignee = model.Assignee;
		_filePaths = (from path in model.Files?.Select((AddTaskAttachmentInfo file) => file.Path)
			where !string.IsNullOrEmpty(path)
			select path).Distinct().ToList() ?? new List<string>();
		_attachmentMarkdown = TaskUtils.GetAttachmentMarkdown(model.ContentText);
		SetProjectTokenValue(projectTokenValue);
	}

	public string RestoreAttachmentMarkdown(string content)
	{
		return TaskUtils.RestoreAttachmentMarkdown(content, _attachmentMarkdown, _contentBeforeCalendar, _calendarContentAtConvert);
	}

	public void CacheAttachmentContentForCalendar(string originalContent, string calendarContent)
	{
		_attachmentMarkdown = TaskUtils.GetAttachmentMarkdown(originalContent);
		_contentBeforeCalendar = originalContent ?? string.Empty;
		_calendarContentAtConvert = calendarContent ?? string.Empty;
	}

	public void SetCalendarFields(string calendarId, string calendarName, string accountId, string location)
	{
		if (calendarId != null)
		{
			_calendarId = calendarId;
		}
		if (calendarName != null)
		{
			_calendarName = calendarName;
		}
		if (accountId != null)
		{
			_accountId = accountId;
		}
		if (location != null)
		{
			_location = location;
		}
	}

	public void KeepCalendarFieldsFrom(AddTaskKindConvertCache other)
	{
		if (other != null)
		{
			if (string.IsNullOrEmpty(_calendarId))
			{
				_calendarId = other._calendarId;
			}
			if (string.IsNullOrEmpty(_calendarName))
			{
				_calendarName = other._calendarName;
			}
			if (string.IsNullOrEmpty(_accountId))
			{
				_accountId = other._accountId;
			}
			if (_location == null)
			{
				_location = other._location;
			}
		}
	}

	public void ApplyCalendarTo(AddTaskViewModel model)
	{
		if (model != null)
		{
			if (!string.IsNullOrEmpty(_calendarId))
			{
				model.CalendarId = _calendarId;
			}
			if (!string.IsNullOrEmpty(_calendarName))
			{
				model.CalendarName = _calendarName;
			}
			if (!string.IsNullOrEmpty(_accountId))
			{
				model.AccountId = _accountId;
			}
			model.NotifyProjectChanged();
		}
	}
}
