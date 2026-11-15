using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.ViewModels;

public class TaskDetailPrintViewModel
{
	public string Title { get; set; }

	public string ContentOrDesc { get; set; }

	public string Kind { get; set; }

	public string AttendId { get; set; }

	public string DayText { get; set; }

	public string RepeatFlag { get; set; }

	public string RepeatFrom { get; set; }

	public BitmapImage Avatar { get; set; }

	public string TaskId { get; set; }

	public string ProjectId { get; set; }

	public int Status { get; set; }

	public int Mode { get; set; }

	public int Priority { get; set; }

	public int? Progress { get; set; }

	public bool ShowSetTime { get; set; }

	public bool IsContent { get; set; }

	public bool? IsAllDay { get; set; }

	public int ImageMode { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? DueDate { get; set; }

	public List<SubtaskPrintViewModel> SubtaskPrintViewModels { get; set; }

	public List<SubtaskPrintViewModel> RealSubtaskPrintViewModels { get; set; }

	public List<TagPrintModel> Tags { get; set; }

	public List<AttachmentViewModel> Attachments { get; set; }

	public List<CommentViewModel> Comments { get; set; }

	public TaskDetailPrintViewModel()
	{
	}

	public TaskDetailPrintViewModel(TaskDetailViewModel model)
	{
		TaskId = model.TaskId;
		Title = model.Title;
		Kind = model.Kind;
		Status = model.Status;
		IsContent = Kind != "CHECKLIST";
		ContentOrDesc = (IsContent ? model.TaskContent : model.Desc);
		AttendId = model.AttendId;
		DayText = model.DayText;
		RepeatFlag = model.RepeatFlag;
		RepeatFrom = model.RepeatFrom;
		ProjectId = model.ProjectId;
		Progress = model.Progress;
		ShowSetTime = model.ShowSetTime;
		IsAllDay = model.IsAllDay;
		StartDate = model.StartDate;
		DueDate = model.DueDate;
		Priority = model.Priority;
		ImageMode = model.ImageMode;
		Tags = LoadTags(model.Tag);
	}

	private List<TagPrintModel> LoadTags(string tagString)
	{
		List<TagPrintModel> list = new List<TagPrintModel>();
		List<string> list2 = TagSerializer.ToTags(tagString);
		List<TagModel> tags = CacheManager.GetTags();
		foreach (string tagName in list2)
		{
			TagModel tagModel = tags.FirstOrDefault((TagModel m) => m.name == tagName);
			if (tagModel != null)
			{
				TagPrintModel tagPrintModel = new TagPrintModel
				{
					TagTitle = tagModel.GetDisplayName(),
					TagBackColor = ((!string.IsNullOrEmpty(tagModel.color)) ? ThemeUtil.GetColorInString(tagModel.color) : ThemeUtil.GetColor("PrimaryColor")),
					TagTextColor = ThemeUtil.GetColorInString("#191919")
				};
				tagPrintModel.TagBackColor = new SolidColorBrush
				{
					Color = tagPrintModel.TagBackColor.Color,
					Opacity = 0.56
				};
				list.Add(tagPrintModel);
			}
		}
		return list;
	}

	public async Task LoadAttachment()
	{
		AttachmentProvider attachmentProvider = new AttachmentProvider(await AttachmentDao.GetTaskAttachments(TaskId), ImageMode);
		Attachments = attachmentProvider.GetModels()?.Where((AttachmentViewModel a) => a.AttachmentModel.status == 0 && !a.AttachmentModel.deleted).ToList();
	}
}
