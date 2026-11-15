using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Framework.Markdown;
using ticktick_WPF.Models;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Summary;

public class SummaryTaskViewModel
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> _003C_003E9__113_0;

		public static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> _003C_003E9__114_0;

		public static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> _003C_003E9__115_0;

		public static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> _003C_003E9__116_0;

		public static Func<TagModel, bool> _003C_003E9__118_1;

		public static Func<TagModel, long> _003C_003E9__118_2;

		public static Func<TagModel, string> _003C_003E9__118_3;

		public static Func<string, string> _003C_003E9__118_5;

		public static Func<SummaryDisplayItemViewModel, long> _003C_003E9__136_0;

		public static Func<SummaryDisplayItemViewModel, long> _003C_003E9__137_0;

		public static Func<SummaryDisplayItemViewModel, long> _003C_003E9__139_0;

		public static Func<ProjectModel, bool> _003C_003E9__140_1;

		public static Func<SummaryDisplayItemViewModel, long> _003C_003E9__140_2;

		public static Func<SummaryDisplayItemViewModel, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>> _003C_003E9__140_3;

		public static Func<Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>, bool> _003C_003E9__140_4;

		internal async Task _003CGetCheckItemBuildProcessorByKey_003Eb__113_0(SummaryTaskViewModel a, SummaryFilterViewModel b)
		{
			await Task.CompletedTask;
		}

		internal async Task _003CGetCalendarBuildProcessorByKey_003Eb__114_0(SummaryTaskViewModel a, SummaryFilterViewModel b)
		{
			await Task.CompletedTask;
		}

		internal async Task _003CGetHabitBuildProcessorByKey_003Eb__115_0(SummaryTaskViewModel a, SummaryFilterViewModel b)
		{
			await Task.CompletedTask;
		}

		internal async Task _003CGetTaskBuildProcessorByKey_003Eb__116_0(SummaryTaskViewModel a, SummaryFilterViewModel b)
		{
			await Task.CompletedTask;
		}

		internal bool _003CBuildTag_003Eb__118_1(TagModel tag)
		{
			return tag != null;
		}

		internal long _003CBuildTag_003Eb__118_2(TagModel tag)
		{
			return tag.sortOrder;
		}

		internal string _003CBuildTag_003Eb__118_3(TagModel tag)
		{
			return tag.label ?? tag.name;
		}

		internal string _003CBuildTag_003Eb__118_5(string tag)
		{
			return "#" + tag;
		}

		internal long _003CBuildHabit_003Eb__136_0(SummaryDisplayItemViewModel item)
		{
			return item.SortOrder;
		}

		internal long _003CBuildHabit_003Eb__137_0(SummaryDisplayItemViewModel item)
		{
			return item.SortOrder;
		}

		internal long _003CBuildCalendar_003Eb__139_0(SummaryDisplayItemViewModel item)
		{
			return item.SortOrder;
		}

		internal bool _003CBuild_003Eb__140_1(ProjectModel p)
		{
			return p.Isinbox;
		}

		internal long _003CBuild_003Eb__140_2(SummaryDisplayItemViewModel item)
		{
			return item.SortOrder;
		}

		internal Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> _003CBuild_003Eb__140_3(SummaryDisplayItemViewModel item)
		{
			return GetTaskBuildProcessorByKey(item.Key);
		}

		internal bool _003CBuild_003Eb__140_4(Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> taskBuilder)
		{
			return taskBuilder != null;
		}
	}

	private const int DetailContentMaxLength = 300;

	private static readonly Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>> TaskBuilders = new Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>>
	{
		{ "project", BuildProject },
		{ "dueDate", BuildTaskDate },
		{ "tag", BuildTag },
		{ "progress", BuildProgress },
		{ "completedTime", BuildCompletedDate },
		{ "focus", BuildFocus },
		{ "title", BuildTitle },
		{ "detail", BuildContent }
	};

	private static readonly Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>> HabitBuilders = new Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>>
	{
		{ "title", BuildSummaryTitle },
		{ "dueDate", BuildTaskDate },
		{ "project", BuildHabitProject },
		{ "completedTime", BuildCompletedDate },
		{ "progress", BuildHabitProgress },
		{ "detail", BuildDesc }
	};

	private static readonly Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>> CalendarBuilders = new Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>>
	{
		{ "title", BuildSummaryTitle },
		{ "dueDate", BuildTaskDate },
		{ "project", BuildCalendarProject },
		{ "completedTime", BuildCalendarCompletedDate },
		{ "detail", BuildDesc }
	};

	private static readonly Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>> CheckItemBuilders = new Dictionary<string, Func<SummaryTaskViewModel, SummaryFilterViewModel, Task>>
	{
		{ "title", BuildSummaryTitle },
		{ "dueDate", BuildTaskDate },
		{ "completedTime", BuildCompletedDate }
	};

	public string EntityId { get; set; }

	public TaskBaseViewModel SourceViewModel { get; set; }

	public DateTime? StartDate { get; set; }

	public DateTime? CompletedDate { get; set; }

	public DateTime? DueDate { get; set; }

	public string Content { get; set; }

	public ProjectModel Project { get; set; }

	public List<string> Tags { get; set; }

	public int Status { get; set; }

	public int? Progress { get; set; }

	public string ProgressText { get; set; }

	public bool? IsAllDay { get; set; }

	public int Priority { get; set; }

	public long SortOrder { get; set; }

	public string GroupName { get; set; }

	public DateTime? GroupDate { get; set; }

	public List<SummaryTaskViewModel> Children { get; set; } = new List<SummaryTaskViewModel>();

	public string Assignee { get; set; }

	public TagModel PrimaryTag { get; set; }

	public string ParentTitle { get; set; }

	public string Title { get; set; }

	public string ColumnName { get; set; }

	public string Desc { get; set; }

	public string Category { get; set; }

	public object Entity { get; set; }

	public int Level { get; set; }

	public List<SummaryDisplayItemModel> DisplayItems { get; set; } = new List<SummaryDisplayItemModel>();

	private static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> GetCheckItemBuildProcessorByKey(string key)
	{
		Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> func;
		if (!CheckItemBuilders.TryGetValue(key, out var value))
		{
			func = _003C_003Ec._003C_003E9__113_0;
			if (func == null)
			{
				return _003C_003Ec._003C_003E9__113_0 = async delegate
				{
					await Task.CompletedTask;
				};
			}
		}
		else
		{
			func = value;
		}
		return func;
	}

	private static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> GetCalendarBuildProcessorByKey(string key)
	{
		Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> func;
		if (!CalendarBuilders.TryGetValue(key, out var value))
		{
			func = _003C_003Ec._003C_003E9__114_0;
			if (func == null)
			{
				return _003C_003Ec._003C_003E9__114_0 = async delegate
				{
					await Task.CompletedTask;
				};
			}
		}
		else
		{
			func = value;
		}
		return func;
	}

	private static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> GetHabitBuildProcessorByKey(string key)
	{
		Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> func;
		if (!HabitBuilders.TryGetValue(key, out var value))
		{
			func = _003C_003Ec._003C_003E9__115_0;
			if (func == null)
			{
				return _003C_003Ec._003C_003E9__115_0 = async delegate
				{
					await Task.CompletedTask;
				};
			}
		}
		else
		{
			func = value;
		}
		return func;
	}

	private static Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> GetTaskBuildProcessorByKey(string key)
	{
		Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> func;
		if (!TaskBuilders.TryGetValue(key, out var value))
		{
			func = _003C_003Ec._003C_003E9__116_0;
			if (func == null)
			{
				return _003C_003Ec._003C_003E9__116_0 = async delegate
				{
					await Task.CompletedTask;
				};
			}
		}
		else
		{
			func = value;
		}
		return func;
	}

	private static async Task BuildTaskDate(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (!filter.ShowTaskDate || !model.StartDate.HasValue)
		{
			return;
		}
		bool flag = filter.ShowTaskTime && model.IsAllDay != true;
		if (filter.SortBy == SummarySortType.dueDate)
		{
			if (flag)
			{
				model.Content = model.Content + "[" + DateUtils.FormatSummaryTimeOnly(model.StartDate.Value) + "] ";
			}
		}
		else
		{
			model.Content = model.Content + "[" + DateUtils.FormatSummaryTime(model.StartDate.Value, flag) + "] ";
		}
	}

	private static async Task BuildTag(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (!filter.ShowTag || model.Tags == null || model.Tags.Count <= 0)
		{
			return;
		}
		List<TagModel> tags = CacheManager.GetTags();
		List<string> source = (from name in model.Tags
			select tags.FirstOrDefault((TagModel tag) => tag.name.Equals(name, StringComparison.OrdinalIgnoreCase)) into tag
			where tag != null
			orderby tag.sortOrder
			select tag.label ?? tag.name).ToList();
		model.Content = model.Content + string.Join(" ", source.Select((string tag) => "#" + tag)) + " ";
	}

	private static async Task BuildHabitProgress(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (filter.ShowProgress && !string.IsNullOrEmpty(model.ProgressText))
		{
			model.Content = model.Content + "[" + model.ProgressText + "] ";
		}
	}

	private static async Task BuildProgress(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (model.Status == 0 && filter.ShowProgress && model.Progress.HasValue && model.Progress != 0)
		{
			model.Content += $"[{model.Progress}%] ";
		}
	}

	private static async Task BuildCalendarCompletedDate(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (filter.ShowCompleteDate && model.CompletedDate.HasValue && filter.SortBy != SummarySortType.completedTime)
		{
			model.Content = model.Content + "[" + DateUtils.FormatSummaryTime(model.CompletedDate.Value, withTime: false) + "] ";
		}
	}

	private static async Task BuildCompletedDate(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (!filter.ShowCompleteDate || !model.CompletedDate.HasValue || model.Status == 0)
		{
			return;
		}
		if (filter.SortBy == SummarySortType.completedTime)
		{
			if (filter.ShowCompletedTime)
			{
				model.Content = model.Content + "[" + DateUtils.FormatSummaryTimeOnly(model.CompletedDate.Value) + "] ";
			}
		}
		else
		{
			model.Content = model.Content + "[" + DateUtils.FormatSummaryTime(model.CompletedDate.Value, filter.ShowCompletedTime) + "] ";
		}
	}

	private static async Task BuildFocus(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (!filter.ShowPomo)
		{
			return;
		}
		var (num, duration, num2) = (await PomoService.GetAdjustPomoSummary(model.SourceViewModel?.Id))?.GetPomoFocusSummary() ?? (0, 0L, 0L);
		if (num > 0 || num2 > 0)
		{
			List<string> list = new List<string>();
			if (num > 0)
			{
				list.Add(Utils.GetString("Pomo") + "×" + num + " " + Utils.GetDurationString(duration, isShort: true, showZero: false, showSpan: false));
			}
			if (num2 > 0)
			{
				list.Add(Utils.GetString("Timing") + ": " + Utils.GetDurationString(num2, isShort: true, showZero: false, showSpan: false));
			}
			model.Content = model.Content + "(" + string.Join(" ", list) + ") ";
		}
	}

	private static async Task BuildHabitProject(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (!filter.ShowProject)
		{
			return;
		}
		string text = Utils.GetString("statistics_habit");
		string text2 = model.ColumnName;
		if (text2.StartsWith("_"))
		{
			switch (text2)
			{
			case "_morning":
			case "_afternoon":
			case "_night":
				text2 = Utils.GetString("HabitSection" + text2.Substring(1, 1).ToUpper() + text2.Substring(2));
				break;
			}
		}
		if (filter.SortBy == SummarySortType.project)
		{
			if (filter.ShowColumn && !string.IsNullOrEmpty(text2))
			{
				model.Content = model.Content + "<" + text2 + "> ";
			}
		}
		else if (filter.ShowColumn && !string.IsNullOrEmpty(text2))
		{
			model.Content = model.Content + "<" + text + "-" + text2 + "> ";
		}
		else
		{
			model.Content = model.Content + "<" + text + "> ";
		}
	}

	private static async Task BuildCalendarProject(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (filter.ShowProject)
		{
			model.Content = model.Content + "<" + model.Project.name + "> ";
		}
	}

	private static async Task BuildProject(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (!filter.ShowProject || model.Project == null || model.Level != 0)
		{
			return;
		}
		string text = (model.Project.Isinbox ? Utils.GetString("Inbox") : model.Project.name);
		string text2 = model.SourceViewModel.ColumnName ?? Utils.GetString("NotSectioned");
		if (filter.SortBy == SummarySortType.project)
		{
			if (filter.ShowColumn && !string.IsNullOrEmpty(text2))
			{
				model.Content = model.Content + "<" + text2 + "> ";
			}
		}
		else if (filter.ShowColumn && !string.IsNullOrEmpty(text2))
		{
			model.Content = model.Content + "<" + text + "-" + text2 + "> ";
		}
		else
		{
			model.Content = model.Content + "<" + text + "> ";
		}
	}

	private static async Task BuildSummaryTitle(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		model.Content = model.Content + model.Title + " ";
	}

	private static async Task BuildTitle(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		TaskBaseViewModel sourceViewModel = model.SourceViewModel;
		string title = sourceViewModel.Title;
		string text = ((title != null && title.Length >= 300) ? sourceViewModel.Title.Substring(0, 300) : sourceViewModel.Title);
		if (filter.ShowParent)
		{
			string parentTitle = model.ParentTitle;
			if (!string.IsNullOrEmpty(parentTitle))
			{
				text = ((!filter.ParentPrefix) ? (text + " /" + parentTitle) : (parentTitle + "/ " + text));
			}
		}
		model.Content = model.Content + text + " ";
	}

	private static async Task BuildDesc(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (filter.ShowDetail && !string.IsNullOrEmpty(model.Desc))
		{
			model.Content += BuildPlainDetailContent(model.Desc, "    [&tabs]");
		}
	}

	private static async Task BuildContent(SummaryTaskViewModel model, SummaryFilterViewModel filter)
	{
		if (!filter.ShowDetail)
		{
			return;
		}
		string text = "[&tabs]";
		TaskBaseViewModel sourceViewModel = model.SourceViewModel;
		string text2 = "    " + text;
		if (sourceViewModel.Kind == "TEXT")
		{
			string text3 = TaskUtils.ReplaceAttachmentTextInString(sourceViewModel.Content, withTypeName: false);
			model.Content += BuildPlainDetailContent(text3, text2);
			return;
		}
		model.Content += BuildPlainDetailContent(sourceViewModel.Desc, text2);
		List<TaskBaseViewModel> checkItemsByTaskId = TaskDetailItemCache.GetCheckItemsByTaskId(sourceViewModel.Id);
		if (checkItemsByTaskId == null || checkItemsByTaskId.Count <= 0)
		{
			return;
		}
		TaskDetailItemDao.SortItems(checkItemsByTaskId);
		foreach (TaskBaseViewModel item in checkItemsByTaskId)
		{
			SummaryTaskViewModel summaryTaskViewModel = BuildCheckItem(item, filter);
			model.Content = model.Content + "\r\n" + text2 + summaryTaskViewModel.Content;
		}
	}

	private static string BuildPlainDetailContent(string text, string linePrefix)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		string text2 = "\r\n" + linePrefix;
		string text3 = TaskUtils.GetContentMarkdownDisplayText(text).Replace("\r\n", " ").Replace('\r', ' ')
			.Replace('\n', ' ');
		int num = Math.Max(0, 300 - text2.Length);
		if (text3.Length > num)
		{
			text3 = text3.Substring(0, num);
		}
		return text2 + MarkdownSemanticConverter.EscapePlainTextLineForPersistence(text3);
	}

	public bool IsHabitModel()
	{
		return GetEntityType() == "habit";
	}

	public bool IsCalendarModel()
	{
		return GetEntityType() == "calendar";
	}

	public bool IsTaskModel()
	{
		return GetEntityType() == "task";
	}

	public string GetEntityType()
	{
		object entity = Entity;
		if (!(entity is TaskModel))
		{
			if (!(entity is HabitModel))
			{
				if (entity is CalendarEventModel)
				{
					return "calendar";
				}
				return "task";
			}
			return "habit";
		}
		return "task";
	}

	public static SummaryTaskViewModel BuildHabit(HabitModel habit, HabitCheckInModel habitCheckIn, Dictionary<string, HabitRecordModel> habit2Note, Dictionary<string, string> sectionId2Name, SummaryFilterViewModel filter)
	{
		string text = "[&tabs]";
		SummaryTaskViewModel summaryTaskViewModel = new SummaryTaskViewModel
		{
			Progress = (int)(habitCheckIn.Value * 1.0 / habit.Goal * 1.0 * 100.0)
		};
		if (habitCheckIn.Value > 0.0 && summaryTaskViewModel.Progress == 0)
		{
			summaryTaskViewModel.Progress = 1;
		}
		summaryTaskViewModel.Entity = habit;
		switch (habitCheckIn.CheckStatus)
		{
		case 1:
			summaryTaskViewModel.Status = -1;
			break;
		case 2:
			if (habitCheckIn.Value >= habitCheckIn.Goal)
			{
				summaryTaskViewModel.Status = 2;
			}
			break;
		default:
			summaryTaskViewModel.Status = 0;
			break;
		}
		summaryTaskViewModel.StartDate = DateTime.ParseExact(habitCheckIn.CheckinStamp, "yyyyMMdd", CultureInfo.InvariantCulture);
		summaryTaskViewModel.IsAllDay = true;
		summaryTaskViewModel.DueDate = null;
		summaryTaskViewModel.CompletedDate = null;
		if (habit.Reminders != null && habit.Reminders.Length != 0 && summaryTaskViewModel.StartDate == DateTime.Today)
		{
			summaryTaskViewModel.IsAllDay = false;
			summaryTaskViewModel.StartDate = DateTime.Today;
			summaryTaskViewModel.Entity = habit;
			CalendarDisplayModel calendarDisplayModel = CalendarDisplayModel.Build(habit, 0, summaryTaskViewModel.StartDate.Value, null);
			summaryTaskViewModel.StartDate = calendarDisplayModel.StartDate;
			summaryTaskViewModel.IsAllDay = calendarDisplayModel.IsAllDay;
		}
		summaryTaskViewModel.SourceViewModel = new HabitBaseViewModel(habit)
		{
			StartDate = summaryTaskViewModel.StartDate,
			DueDate = null,
			IsAllDay = summaryTaskViewModel.IsAllDay
		};
		string key = habitCheckIn.HabitId + "_" + habitCheckIn.CheckinStamp;
		if (habit2Note.TryGetValue(key, out var value))
		{
			summaryTaskViewModel.Desc = value.Content;
		}
		if ((habitCheckIn.IsComplete() || habitCheckIn.IsUnComplete()) && habitCheckIn.CheckinTime.HasValue)
		{
			summaryTaskViewModel.CompletedDate = habitCheckIn.CheckinTime.Value;
		}
		double value2 = habitCheckIn.Value;
		if (string.Equals(habit.Type.ToLower(), "real") && value2 > 0.0)
		{
			if (Math.Abs(value2 - Math.Floor(value2)) > 1E-06)
			{
				summaryTaskViewModel.ProgressText = $"{value2:F2}/{habitCheckIn.Goal:F2}";
			}
			else
			{
				summaryTaskViewModel.ProgressText = $"{(int)value2}/{(int)habitCheckIn.Goal}";
			}
		}
		summaryTaskViewModel.Priority = 0;
		summaryTaskViewModel.Title = habit.Name;
		summaryTaskViewModel.Content = "\r\n" + text;
		summaryTaskViewModel.Project = new ProjectModel
		{
			id = "habit",
			name = Utils.GetString("statistics_habit"),
			sortOrder = long.MaxValue
		};
		string text2 = "- ";
		if (filter.ShowStatus)
		{
			text2 = ((habitCheckIn.IsComplete() || habitCheckIn.IsUnComplete()) ? "- [x] " : "- [ ] ");
		}
		summaryTaskViewModel.Content += text2;
		summaryTaskViewModel.ColumnName = Utils.GetString("Others");
		if (sectionId2Name != null && sectionId2Name.TryGetValue(habit.SectionId ?? "", out var value3))
		{
			summaryTaskViewModel.ColumnName = value3;
		}
		foreach (SummaryDisplayItemViewModel item in filter.DisplayItems.OrderBy((SummaryDisplayItemViewModel item) => item.SortOrder).ToList())
		{
			GetHabitBuildProcessorByKey(item.Key)(summaryTaskViewModel, filter);
		}
		return summaryTaskViewModel;
	}

	public static SummaryTaskViewModel BuildHabit(HabitModel habit, Dictionary<string, string> sectionId2Name, SummaryFilterViewModel filter)
	{
		string text = "[&tabs]";
		SummaryTaskViewModel summaryTaskViewModel = new SummaryTaskViewModel();
		summaryTaskViewModel.StartDate = DateTime.Today;
		summaryTaskViewModel.Entity = habit;
		CalendarDisplayModel calendarDisplayModel = CalendarDisplayModel.Build(habit, 0, summaryTaskViewModel.StartDate.Value, null);
		summaryTaskViewModel.StartDate = calendarDisplayModel.StartDate;
		summaryTaskViewModel.IsAllDay = calendarDisplayModel.IsAllDay;
		summaryTaskViewModel.SourceViewModel = new HabitBaseViewModel(habit)
		{
			StartDate = summaryTaskViewModel.StartDate,
			DueDate = null,
			IsAllDay = summaryTaskViewModel.IsAllDay
		};
		summaryTaskViewModel.DueDate = null;
		summaryTaskViewModel.CompletedDate = null;
		summaryTaskViewModel.Priority = 0;
		summaryTaskViewModel.Title = habit.Name;
		summaryTaskViewModel.Content = "\r\n" + text;
		summaryTaskViewModel.Project = new ProjectModel
		{
			id = "habit",
			name = Utils.GetString("statistics_habit"),
			sortOrder = long.MaxValue
		};
		string text2 = "- ";
		if (filter.ShowStatus)
		{
			text2 = "- [ ] ";
		}
		summaryTaskViewModel.Content += text2;
		summaryTaskViewModel.ColumnName = Utils.GetString("Others");
		if (sectionId2Name != null && sectionId2Name.TryGetValue(habit.SectionId ?? "", out var value))
		{
			summaryTaskViewModel.ColumnName = value;
		}
		foreach (SummaryDisplayItemViewModel item in filter.DisplayItems.OrderBy((SummaryDisplayItemViewModel item) => item.SortOrder).ToList())
		{
			GetHabitBuildProcessorByKey(item.Key)(summaryTaskViewModel, filter);
		}
		return summaryTaskViewModel;
	}

	public static SummaryTaskViewModel BuildCheckItem(TaskBaseViewModel item, SummaryFilterViewModel filter)
	{
		SummaryTaskViewModel summaryTaskViewModel = new SummaryTaskViewModel
		{
			Entity = item,
			Title = item.Title,
			StartDate = item.StartDate,
			IsAllDay = item.IsAllDay,
			CompletedDate = item.CompletedTime,
			Status = item.Status
		};
		string text = "- ";
		if (filter.ShowStatus)
		{
			text = ((summaryTaskViewModel.Status == 0) ? "- [ ] " : "- [x] ");
		}
		summaryTaskViewModel.Content += text;
		foreach (SummaryDisplayItemViewModel item2 in filter.DisplayItems.OrderBy((SummaryDisplayItemViewModel it) => item.SortOrder).ToList())
		{
			GetCheckItemBuildProcessorByKey(item2.Key)(summaryTaskViewModel, filter);
		}
		return summaryTaskViewModel;
	}

	public static SummaryTaskViewModel BuildCalendar(CalendarDisplayModel calendarEvent, Dictionary<string, string> id2Name, SummaryFilterViewModel filter)
	{
		string text = "[&tabs]";
		SummaryTaskViewModel summaryTaskViewModel = new SummaryTaskViewModel();
		summaryTaskViewModel.Entity = calendarEvent;
		summaryTaskViewModel.StartDate = calendarEvent.StartDate;
		summaryTaskViewModel.DueDate = calendarEvent.DueDate;
		summaryTaskViewModel.Desc = calendarEvent.SourceViewModel?.Desc;
		summaryTaskViewModel.IsAllDay = calendarEvent.IsAllDay;
		summaryTaskViewModel.Priority = 0;
		summaryTaskViewModel.Title = calendarEvent.Title;
		summaryTaskViewModel.Content = "\r\n" + text;
		summaryTaskViewModel.SourceViewModel = new TaskBaseViewModel
		{
			StartDate = summaryTaskViewModel.StartDate,
			DueDate = summaryTaskViewModel.DueDate,
			IsAllDay = summaryTaskViewModel.IsAllDay
		};
		string value;
		string name = (id2Name.TryGetValue(calendarEvent.SourceViewModel?.CalendarId ?? "", out value) ? value : Utils.GetString("Calendar"));
		summaryTaskViewModel.Project = new ProjectModel
		{
			id = "calendar",
			name = name,
			sortOrder = 9223372036854775806L
		};
		if ((calendarEvent.DueDate.HasValue && calendarEvent.DueDate.Value.Date < DateTime.Today) || (!calendarEvent.DueDate.HasValue && calendarEvent.StartDate.HasValue && calendarEvent.StartDate < DateTime.Today) || calendarEvent.Status == 2)
		{
			summaryTaskViewModel.Status = 2;
			if (calendarEvent.DueDate.HasValue)
			{
				summaryTaskViewModel.CompletedDate = calendarEvent.DueDate.Value.Date;
			}
			else if (calendarEvent.StartDate.HasValue)
			{
				summaryTaskViewModel.CompletedDate = calendarEvent.StartDate.Value.Date;
			}
		}
		else
		{
			summaryTaskViewModel.Status = 0;
		}
		string text2 = "- ";
		if (filter.ShowStatus)
		{
			text2 = ((summaryTaskViewModel.Status == 0) ? "- [ ] " : "- [x] ");
		}
		summaryTaskViewModel.Content += text2;
		foreach (SummaryDisplayItemViewModel item in filter.DisplayItems.OrderBy((SummaryDisplayItemViewModel item) => item.SortOrder).ToList())
		{
			GetCalendarBuildProcessorByKey(item.Key)(summaryTaskViewModel, filter);
		}
		return summaryTaskViewModel;
	}

	public static async Task<SummaryTaskViewModel> Build(Node<TaskBaseViewModel> node, SummaryFilterViewModel filter, int level = 0)
	{
		string tabs = "[&tabs]";
		for (int i = 0; i < level; i++)
		{
			tabs += "    ";
		}
		TaskBaseViewModel task = node.Value;
		SummaryTaskViewModel model = new SummaryTaskViewModel
		{
			Level = level,
			SourceViewModel = task,
			Entity = task,
			StartDate = task.StartDate,
			DueDate = task.DueDate,
			CompletedDate = task.CompletedTime,
			Status = task.Status,
			Priority = task.Priority,
			Progress = task.Progress,
			SortOrder = task.SortOrder,
			IsAllDay = task.IsAllDay,
			Assignee = task.Assignee,
			Tags = task.Tags?.ToList()
		};
		if (level == 0 && !string.IsNullOrEmpty(task.ParentId))
		{
			TaskModel taskModel = await TaskDao.GetTaskById(task.ParentId);
			if (taskModel != null)
			{
				model.ParentTitle = taskModel.title;
			}
		}
		model.Project = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == task.ProjectId) ?? CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.Isinbox) ?? new ProjectModel();
		if (model.Project.Isinbox)
		{
			model.Project.sortOrder = long.MinValue;
		}
		model.Content = "\r\n" + tabs;
		string text = "- ";
		if (filter.ShowStatus)
		{
			text = ((model.Status == 0) ? "- [ ] " : "- [x] ");
		}
		model.Content += text;
		List<SummaryDisplayItemViewModel> source = filter.DisplayItems.OrderBy((SummaryDisplayItemViewModel summaryDisplayItemViewModel) => summaryDisplayItemViewModel.SortOrder).ToList();
		foreach (Func<SummaryTaskViewModel, SummaryFilterViewModel, Task> item2 in from summaryDisplayItemViewModel in source
			select GetTaskBuildProcessorByKey(summaryDisplayItemViewModel.Key) into taskBuilder
			where taskBuilder != null
			select taskBuilder)
		{
			await item2(model, filter);
		}
		if (model.Content.EndsWith(" "))
		{
			model.Content = model.Content.Substring(0, model.Content.Length - 1);
		}
		if (node.Children != null)
		{
			foreach (Node<TaskBaseViewModel> child in node.Children)
			{
				SummaryTaskViewModel item = await Build(child, filter, level + 1);
				model.Children.Add(item);
			}
		}
		return model;
	}

	public string GetContent(bool withSpace)
	{
		return MarkdownUtils.RemoveFoldedMetadataComments(Content.Replace("[&tabs]", withSpace ? "    " : ""));
	}
}
