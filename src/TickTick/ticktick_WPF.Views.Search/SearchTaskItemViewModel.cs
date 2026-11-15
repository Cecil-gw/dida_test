using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

public class SearchTaskItemViewModel : SearchItemBaseModel
{
	private int _status;

	public bool TogglingStatus;

	private string _attachmentContent;

	public override bool CanSelect { get; set; } = true;

	public Geometry Icon => ThemeUtil.GetTaskIconGeometry(SourceModel.Type, Status, SourceModel.Kind, SubscribeCalendarHelper.GetCalendarType(SourceModel.CalendarId));

	public double IconWidth => (SourceModel.Type == DisplayType.Course) ? 15 : 14;

	public TaskBaseViewModel SourceModel { get; set; }

	public string CommentStr { get; set; }

	public string Content { get; set; }

	public int Status
	{
		get
		{
			return _status;
		}
		set
		{
			if (_status != value)
			{
				_status = value;
				OnPropertyChanged("Status");
				OnPropertyChanged("Icon");
			}
		}
	}

	public SearchKind Kind { get; set; }

	public bool IsNormal => Kind == SearchKind.Normal;

	public bool IsTask => Kind == SearchKind.Task;

	public bool IsParents => Kind == SearchKind.Parents;

	public string DateText { get; set; }

	public bool HasDate => !string.IsNullOrEmpty(DateText);

	public bool IsOverDue { get; set; }

	public string ParentsCountText { get; set; }

	public SearchTaskItemViewModel(TaskBaseViewModel task)
	{
		SourceModel = task;
		Status = task.Status;
	}

	public void InitData(SearchKind kind)
	{
		Kind = kind;
		switch (kind)
		{
		case SearchKind.Normal:
			SetComment();
			SetAttachment();
			SetContent();
			break;
		case SearchKind.Task:
			SetDateText();
			break;
		case SearchKind.Parents:
			CanSelect = false;
			break;
		}
	}

	public void SetParentsCount(int count)
	{
		if (count > 1)
		{
			ParentsCountText = string.Format(Utils.GetString("AndMoreTasks"), count - 1);
		}
		else
		{
			ParentsCountText = string.Empty;
		}
	}

	private void SetDateText()
	{
		TaskBaseViewModel sourceModel = SourceModel;
		if (sourceModel != null && sourceModel.StartDate.HasValue)
		{
			DateText = DateUtils.FormatListDateString(SourceModel.StartDate.Value, SourceModel.DueDate, SourceModel.IsAllDay);
			IsOverDue = DateUtils.IsOutDated(SourceModel.StartDate.Value, SourceModel.DueDate, SourceModel.IsAllDay);
		}
	}

	private void SetAttachment()
	{
		if (SourceModel != null)
		{
			_attachmentContent = TaskUtils.ReplaceAttachmentNameInString(SourceModel.Content);
		}
	}

	private void SetComment()
	{
		string commentStr = string.Empty;
		List<CommentModel> taskComments = TaskCommentCache.GetTaskComments(SourceModel.Id);
		if (taskComments != null && taskComments.Count > 0)
		{
			foreach (CommentModel item in from c in taskComments
				orderby c.createdTime descending, c.modifiedTime descending
				select c)
			{
				Match match = SearchHelper.PreSearchRegex.Match(item.title?.ToLower() ?? string.Empty);
				if (item.title != null && match.Success)
				{
					commentStr = ((match.Index > 8) ? ("..." + item.title.Substring(match.Index - 8).Replace("\r\n", " ").Replace("\r", " ")
						.Replace("\n", " ")) : item.title);
					break;
				}
			}
		}
		CommentStr = commentStr;
	}

	private void SetContent()
	{
		string text = null;
		if (SourceModel.Kind == "CHECKLIST")
		{
			Match match = SearchHelper.PreSearchRegex.Match(SourceModel.Desc?.ToLower() ?? string.Empty);
			if (SourceModel.Desc != null && match.Success)
			{
				text = ((match.Index > 8) ? ("..." + SourceModel.Desc.Substring(match.Index - 8).Replace("\r\n", " ").Replace("\r", " ")
					.Replace("\n", " ")) : SourceModel.Desc);
			}
			else
			{
				List<TaskBaseViewModel> checkItemsByTaskId = TaskDetailItemCache.GetCheckItemsByTaskId(SourceModel.Id);
				if (checkItemsByTaskId != null)
				{
					checkItemsByTaskId.Sort((TaskBaseViewModel a, TaskBaseViewModel b) => a.SortOrder.CompareTo(b.SortOrder));
					foreach (TaskBaseViewModel item in checkItemsByTaskId)
					{
						Match match2 = SearchHelper.PreSearchRegex.Match(item.Title?.ToLower() ?? string.Empty);
						if (item.Title != null && match2.Success)
						{
							text = "- " + ((match2.Index > 8) ? ("..." + item.Title.Substring(match2.Index - 8)) : item.Title);
							break;
						}
					}
					if (string.IsNullOrEmpty(text))
					{
						if (!string.IsNullOrEmpty(SourceModel.Desc))
						{
							text = SourceModel.Desc;
						}
						else
						{
							TaskBaseViewModel taskBaseViewModel = checkItemsByTaskId.FirstOrDefault((TaskBaseViewModel i) => i.Status == 0) ?? checkItemsByTaskId.FirstOrDefault();
							text = (string.IsNullOrEmpty(taskBaseViewModel?.Title) ? string.Empty : ("- " + taskBaseViewModel.Title));
						}
					}
				}
			}
		}
		else if (!string.IsNullOrEmpty(_attachmentContent))
		{
			string attachmentContent = _attachmentContent;
			attachmentContent = TaskUtils.CleanMarkdownMarksInString(attachmentContent);
			Match match3 = SearchHelper.PreSearchRegex.Match(attachmentContent.ToLower());
			text = ((!match3.Success || match3.Index <= 11) ? attachmentContent : ("..." + attachmentContent.Substring(match3.Index - 10)));
		}
		else
		{
			text = string.Empty;
		}
		Content = text;
	}

	public void ResetIcon()
	{
		OnPropertyChanged("Icon");
	}
}
