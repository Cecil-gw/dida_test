using System;
using System.Collections.Generic;

namespace ticktick_WPF.ViewModels;

public class TaskPrintViewModel : BaseViewModel
{
	public string Title { get; set; }

	public string Content { get; set; }

	public string Desc { get; set; }

	public string AvatarUrl { get; set; }

	public string FirstTag { get; set; }

	public string SecondTag { get; set; }

	public string MoreTag { get; set; }

	public string ProjectName { get; set; }

	public DisplayType Type { get; set; }

	public string Kind { get; set; }

	public int Progress { get; set; }

	public int Status { get; set; }

	public int Count { get; set; }

	public int Priority { get; set; }

	public bool ShowAttachment { get; set; }

	public bool ShowProgress { get; set; }

	public bool ShowLocation { get; set; }

	public bool ShowComment { get; set; }

	public bool ShowDescription { get; set; }

	public bool IsOpen { get; set; }

	public bool ShowRepeat { get; set; }

	public bool ShowReminder { get; set; }

	public string AttendId { get; set; }

	public bool? IsAllDay { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? DueDate { get; set; }

	public List<SubtaskPrintViewModel> SubtaskPrintViewModels { get; set; }

	public bool IsNote { get; set; }

	public int Level { get; set; }

	public int CalendarType { get; set; }

	public TaskPrintViewModel(DisplayItemModel model)
	{
		Title = model.Title;
		AvatarUrl = model.AvatarUrl;
		string[] tags = model.Tags;
		FirstTag = ((tags != null && tags.Length != 0) ? model.Tags[0] : null);
		string[] tags2 = model.Tags;
		SecondTag = ((tags2 != null && tags2.Length > 1) ? model.Tags[1] : null);
		string[] tags3 = model.Tags;
		MoreTag = ((tags3 != null && tags3.Length > 2) ? ("+" + (model.Tags.Length - 2)) : null);
		ProjectName = model.ProjectName;
		Type = model.Type;
		Kind = model.Kind;
		Progress = model.Progress;
		Status = model.Status;
		Priority = model.Priority;
		ShowAttachment = model.GetShowAttachment();
		ShowProgress = model.ShowProgress;
		ShowLocation = model.ShowLocation;
		ShowComment = model.ShowComment;
		ShowDescription = model.ShowDescription;
		ShowRepeat = model.ShowRepeat;
		ShowReminder = model.ShowReminder == true;
		IsAllDay = model.IsAllDay;
		StartDate = model.StartDate;
		DueDate = model.DueDate;
		Count = model.Num;
		IsOpen = model.IsOpen;
		IsNote = model.IsNote;
		Level = model.Level;
		AttendId = model.AttendId;
		CalendarType = model.CalendarType;
	}
}
