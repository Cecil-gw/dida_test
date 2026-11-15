using System;
using System.Collections.Generic;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util;

public class OperationExtra
{
	public string TaskId;

	public List<string> BatchTaskIds;

	public int Priority = -1;

	public string ProjectId = string.Empty;

	public string ColumnId = string.Empty;

	public string Assignee = string.Empty;

	public TimeData TimeModel;

	public TagSelectData Tags;

	public bool ShowCopy;

	public bool ShowCopyLink;

	public bool ShowPomo;

	public bool ShowSkip;

	public bool ShowDate = true;

	public bool ShowAssignTo;

	public bool ShowCreateSubTask;

	public bool ShowLinkParent = true;

	public bool ShowMerge;

	public bool InNoteProject;

	public bool ShowAIComplete;

	public TaskType TaskType;

	public bool? IsAbandoned;

	public bool? IsPinned;

	public DateTime? CompleteTime;

	public bool ShowCopyText;

	public bool InBatch;

	public bool HasUncompletedTask;

	public bool HasCompletedTask;

	public bool HasAbandonedTask;

	public bool InCal;

	public bool CanSwitch;

	public bool ShowSwitch;

	public string FailedSwitchTips = string.Empty;

	public bool CanPostpone;

	public bool CanPostponeTime;

	public bool UsedInBatchControl { get; set; }
}
