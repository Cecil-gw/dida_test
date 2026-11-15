using System;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.ViewModels;

public class RightColumnViewModel
{
	public string AvatarUrl { get; set; }

	public string FirstTag { get; set; }

	public string SecondTag { get; set; }

	public string MoreTag { get; set; }

	public string ProjectName { get; set; }

	public int Progress { get; set; }

	public int Status { get; set; }

	public bool ShowAttachment { get; set; }

	public bool ShowProgress { get; set; }

	public bool ShowLocation { get; set; }

	public bool ShowComment { get; set; }

	public bool ShowDescription { get; set; }

	public bool ShowRepeat { get; set; }

	public bool ShowReminder { get; set; }

	public bool? IsAllDay { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? DueDate { get; set; }

	public bool IsNote { get; set; }

	public RightColumnViewModel(TaskPrintViewModel model, bool hideProjectName)
	{
		AvatarUrl = model.AvatarUrl;
		FirstTag = TagDataHelper.GetTagDisplayName(model.FirstTag);
		SecondTag = TagDataHelper.GetTagDisplayName(model.SecondTag);
		MoreTag = model.MoreTag;
		ProjectName = (hideProjectName ? "" : model.ProjectName);
		Progress = model.Progress;
		Status = model.Status;
		ShowAttachment = model.ShowAttachment;
		ShowProgress = model.ShowProgress;
		ShowLocation = model.ShowLocation;
		ShowComment = model.ShowComment;
		ShowDescription = model.ShowDescription;
		ShowRepeat = model.ShowRepeat;
		ShowReminder = model.ShowReminder;
		IsAllDay = model.IsAllDay;
		StartDate = model.StartDate;
		DueDate = model.DueDate;
		IsNote = model.IsNote;
	}
}
