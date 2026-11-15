using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Views.Summary;

public class SummaryFilterViewModel : BaseViewModel
{
	private string _selectedTemplateId;

	private bool _showTemplate;

	private List<string> _moreConditions = new List<string> { "priority", "assignee" };

	private NormalFilterViewModel _filter;

	private DateFilter _dateFilter = DateFilter.ThisWeek;

	private List<string> _selectedTags = new List<string>();

	private List<string> _selectedProjectGroupIds = new List<string>();

	private List<string> _selectedCalendarAccountIds = new List<string>();

	private List<string> _selectedCalendarIds = new List<string>();

	private List<string> _selectedProjectIds = new List<string> { "ProjectAll2e4c103c57ef480997943206" };

	private List<string> _selectedPriorities = new List<string>();

	private List<string> _selectedStatus = new List<string>();

	private DateTime? _startDate = Utils.GetWeekStart(DateTime.Today);

	private DateTime? _endDate = Utils.GetWeekStart(DateTime.Today).AddDays(7.0);

	private string _selectedProjectDisplayText = Utils.GetString("AllList");

	private string _selectedTagDisplayText = Utils.GetString("AllTags");

	private string _selectedAssignTypeText = Utils.GetString("AssignTo");

	private string _selectedPriorityDisplayText = Utils.GetString("all_priorities");

	private string _selectedStatusTypeText = Utils.GetString("AllStatus");

	private SummarySortType _sortBy;

	private bool _showCompleteDate = true;

	private bool _showDetail;

	private bool _showPomo;

	private bool _showProgress = true;

	private bool _showBelongList;

	public string Name => GetSelectedTemplateName();

	public string SelectedTemplateId
	{
		get
		{
			return _selectedTemplateId;
		}
		set
		{
			_selectedTemplateId = value;
			OnPropertyChanged("SelectedTemplateId");
			OnPropertyChanged("Name");
		}
	}

	public bool ShowTemplate
	{
		get
		{
			return _showTemplate;
		}
		set
		{
			_showTemplate = value;
			OnPropertyChanged("ShowTemplate");
		}
	}

	public bool ShowMoreConditions => _moreConditions.Count > 0;

	public List<string> MoreConditions
	{
		get
		{
			return _moreConditions;
		}
		set
		{
			_moreConditions = value;
			OnPropertyChanged("MoreConditions");
			OnPropertyChanged("ShowMoreConditions");
			OnPropertyChanged("ShowPriority");
			OnPropertyChanged("ShowAssignee");
		}
	}

	public NormalFilterViewModel Filter
	{
		get
		{
			return _filter;
		}
		set
		{
			_filter = value;
			OnPropertyChanged("Filter");
		}
	}

	public List<SummaryTemplate> SummaryTemplates { get; set; }

	public string ProItemsText => GetProItemsText();

	public ObservableCollection<SummaryDisplayItemViewModel> DisplayItems { get; set; } = new ObservableCollection<SummaryDisplayItemViewModel>();

	public bool ShowPriority => !MoreConditions.Contains("priority");

	public bool ShowAssignee
	{
		get
		{
			if (!MoreConditions.Contains("assignee"))
			{
				return HasShareList();
			}
			return false;
		}
	}

	public bool WithHabit
	{
		get
		{
			if (_selectedProjectIds.Contains("Habit2e4c103c57ef480997943206") || IsProjectCategoryAll())
			{
				return IsNonProjectRuleAll();
			}
			return false;
		}
	}

	public bool WithCalendar
	{
		get
		{
			if (_selectedProjectIds.Contains("Calendar5959a2259161d16d23a4f272") || IsProjectCategoryAll())
			{
				return IsNonProjectRuleAll();
			}
			return false;
		}
	}

	public SummarySortType SortBy
	{
		get
		{
			return _sortBy;
		}
		set
		{
			_sortBy = value;
		}
	}

	public bool ShowCompleteDate => IsDisplayItemEnabled("completedTime");

	public bool ShowTaskDate => IsDisplayItemEnabled("dueDate");

	public bool ShowTag => IsDisplayItemEnabled("tag");

	public bool ShowProject => IsDisplayItemEnabled("project");

	public bool ShowStatus => IsDisplayItemEnabled("status");

	public bool ShowDetail => IsDisplayItemEnabled("detail");

	public bool ShowProgress => IsDisplayItemEnabled("progress");

	public bool ShowParent => IsDisplayItemEnabled("parentTask");

	public bool ShowColumn
	{
		get
		{
			if (ShowProject)
			{
				return GetStyleByKey("project") == "column";
			}
			return false;
		}
	}

	public bool ShowTaskTime
	{
		get
		{
			if (ShowTaskDate)
			{
				return GetStyleByKey("dueDate") == "time";
			}
			return false;
		}
	}

	public bool ParentPrefix
	{
		get
		{
			if (ShowParent)
			{
				return IsParentBeforeTitle();
			}
			return false;
		}
	}

	public bool ShowCompletedTime
	{
		get
		{
			if (ShowCompleteDate)
			{
				return GetStyleByKey("completedTime") == "time";
			}
			return false;
		}
	}

	public List<string> SelectedProjectIds
	{
		get
		{
			return _selectedProjectIds;
		}
		set
		{
			_selectedProjectIds = value;
			OnPropertyChanged("SelectedProjectIds");
		}
	}

	public List<string> SelectedPriorities
	{
		get
		{
			return _selectedPriorities;
		}
		set
		{
			_selectedPriorities = value;
			OnPropertyChanged("SelectedPriorities");
		}
	}

	public List<string> SelectedStatus
	{
		get
		{
			return _selectedStatus;
		}
		set
		{
			_selectedStatus = value;
			OnPropertyChanged("SelectedStatus");
		}
	}

	public List<string> SelectedProjectGroupIds
	{
		get
		{
			return _selectedProjectGroupIds;
		}
		set
		{
			_selectedProjectGroupIds = value;
			OnPropertyChanged("SelectedProjectGroupIds");
		}
	}

	public List<string> SelectedCalendarAccountIds
	{
		get
		{
			return _selectedCalendarAccountIds ?? new List<string>();
		}
		set
		{
			_selectedCalendarAccountIds = value;
			OnPropertyChanged("SelectedCalendarAccountIds");
		}
	}

	public List<string> SelectedCalendarIds
	{
		get
		{
			return _selectedCalendarIds ?? new List<string>();
		}
		set
		{
			_selectedCalendarIds = value;
			OnPropertyChanged("SelectedCalendarIds");
		}
	}

	public List<string> Assignees { get; set; }

	public string SelectedAssignTypeText
	{
		get
		{
			return _selectedAssignTypeText;
		}
		set
		{
			_selectedAssignTypeText = value;
			OnPropertyChanged("SelectedAssignTypeText");
		}
	}

	public string SelectedTagDisplayText
	{
		get
		{
			return _selectedTagDisplayText;
		}
		set
		{
			_selectedTagDisplayText = value;
			OnPropertyChanged("SelectedTagDisplayText");
		}
	}

	public List<string> SelectedTags
	{
		get
		{
			return _selectedTags;
		}
		set
		{
			_selectedTags = value;
			OnPropertyChanged("SelectedTags");
		}
	}

	public DateFilter DateFilter
	{
		get
		{
			return _dateFilter;
		}
		set
		{
			_dateFilter = value;
			OnPropertyChanged("DateFilter");
		}
	}

	public DateTime? StartDate
	{
		get
		{
			return _startDate;
		}
		set
		{
			_startDate = value;
			OnPropertyChanged("StartDate");
		}
	}

	public DateTime? EndDate
	{
		get
		{
			return _endDate;
		}
		set
		{
			_endDate = value;
			OnPropertyChanged("EndDate");
		}
	}

	public string SelectedProjectDisplayText
	{
		get
		{
			return _selectedProjectDisplayText;
		}
		set
		{
			_selectedProjectDisplayText = value;
			OnPropertyChanged("SelectedProjectDisplayText");
		}
	}

	public string SelectedStatusDisplayText
	{
		get
		{
			return _selectedStatusTypeText;
		}
		set
		{
			_selectedStatusTypeText = value;
			OnPropertyChanged("SelectedStatusDisplayText");
		}
	}

	public string SelectedPriorityDisplayText
	{
		get
		{
			return _selectedPriorityDisplayText;
		}
		set
		{
			_selectedPriorityDisplayText = value;
			OnPropertyChanged("SelectedPriorityDisplayText");
		}
	}

	public bool ShowPomo
	{
		get
		{
			return IsDisplayItemEnabled("focus");
		}
		set
		{
			_showPomo = value;
			OnPropertyChanged("ShowPomo");
		}
	}

	private string GetSelectedTemplateName()
	{
		if (GetSelectedTemplate() == null)
		{
			return Utils.GetString("Summary");
		}
		return GetSelectedTemplate().name;
	}

	private static SummarySortType TryParseSortType(string sortName)
	{
		if (!Enum.TryParse<SummarySortType>(sortName, ignoreCase: true, out var result))
		{
			return SummarySortType.progress;
		}
		return result;
	}

	public async void SaveTemplate(SummaryTemplateViewModel model, Action<SummaryTemplate> handler)
	{
		handler(GetSelectedTemplate(model.Id));
		SummaryTemplates summaryTemplates = LocalSettings.Settings.UserPreference.summaryTemplates;
		SummaryTemplate summaryTemplate = summaryTemplates?.templates?.FirstOrDefault((SummaryTemplate it) => it.id == model.Id);
		if (summaryTemplate != null)
		{
			handler(summaryTemplate);
			summaryTemplates.mtime = Utils.GetNowTimeStampInMills();
			await SettingsHelper.PushLocalPreference();
		}
	}

	public async void AddTemplate(string name, SummaryFilterViewModel viewModel)
	{
		SummaryTemplates summaryTemplates = LocalSettings.Settings.UserPreference.summaryTemplates;
		if (summaryTemplates == null)
		{
			LocalSettings.Settings.UserPreference.summaryTemplates = new SummaryTemplates();
			summaryTemplates = LocalSettings.Settings.UserPreference.summaryTemplates;
		}
		long sortOrder = 0L;
		if (summaryTemplates.templates != null && summaryTemplates.templates.Count > 0)
		{
			sortOrder = summaryTemplates.templates.Max((SummaryTemplate it) => it.sortOrder) + 1;
		}
		NormalFilterViewModel normalFilterViewModel = new NormalFilterViewModel
		{
			Projects = viewModel.SelectedProjectIds.ToList(),
			Groups = viewModel.SelectedProjectGroupIds.ToList(),
			CalendarAccounts = viewModel.SelectedCalendarAccountIds.ToList(),
			Calendars = viewModel.SelectedCalendarIds.ToList(),
			Tags = viewModel.SelectedTags.ToList(),
			Priorities = viewModel.SelectedPriorities.Select(int.Parse).ToList(),
			Status = viewModel.SelectedStatus,
			Assignees = (viewModel.Assignees?.ToList() ?? new List<string>()),
			DueDates = new List<string> { Filter2Rule(DateFilter, StartDate, EndDate) },
			CompletedTimes = new List<string> { Filter2Rule(DateFilter, StartDate, EndDate) },
			Version = 6
		};
		SummaryTemplate summaryTemplate = new SummaryTemplate
		{
			id = Utils.GetGuid(),
			name = name,
			rule = normalFilterViewModel.ToRule(isSave: false),
			sortType = viewModel.SortBy.ToString(),
			displayItems = viewModel.DisplayItems.Select((SummaryDisplayItemViewModel it) => it.ToItemModel()).ToList(),
			sortOrder = sortOrder
		};
		if (summaryTemplates.templates == null)
		{
			summaryTemplates.templates = new List<SummaryTemplate>();
		}
		summaryTemplates.templates.Add(summaryTemplate);
		summaryTemplates.mtime = Utils.GetNowTimeStampInMills();
		SelectedTemplateId = summaryTemplate.id;
		LocalSettings.Settings.ExtraSettings.SelectedSummaryTemplateId = SelectedTemplateId;
		await SettingsHelper.PushLocalPreference();
	}

	private string GetSelectedRule()
	{
		return new NormalFilterViewModel
		{
			Projects = SelectedProjectIds.ToList(),
			Groups = SelectedProjectGroupIds.ToList(),
			CalendarAccounts = SelectedCalendarAccountIds.ToList(),
			Calendars = SelectedCalendarIds.ToList(),
			Tags = SelectedTags.ToList(),
			Priorities = SelectedPriorities.Select(int.Parse).ToList(),
			Status = SelectedStatus,
			Assignees = (Assignees?.ToList() ?? new List<string>()),
			DueDates = new List<string> { Filter2Rule(DateFilter, StartDate, EndDate) },
			CompletedTimes = new List<string> { Filter2Rule(DateFilter, StartDate, EndDate) },
			Version = 6
		}.ToRule(isSave: false);
	}

	public void SaveTemplate(string name = null, bool sync = false)
	{
		SaveSelectedTemplate(delegate(SummaryTemplate template)
		{
			template.name = name ?? GetSelectedTemplateName();
			template.rule = GetSelectedRule();
			template.sortType = SortBy.ToString();
			template.displayItems = DisplayItems.Select((SummaryDisplayItemViewModel it) => it.ToItemModel()).ToList();
		}, sync);
	}

	public async void SaveSelectedTemplate(Action<SummaryTemplate> handler, bool sync = false)
	{
		handler(GetSelectedTemplate(SelectedTemplateId));
		if (SelectedTemplateId == "defaultId")
		{
			SummaryTemplate summaryTemplate = JsonConvert.DeserializeObject<SummaryTemplate>(LocalSettings.Settings.ExtraSettings.DefaultSummaryTemplate);
			summaryTemplate.rule = GetSelectedRule();
			summaryTemplate.sortType = SortBy.ToString();
			summaryTemplate.displayItems = DisplayItems.Select((SummaryDisplayItemViewModel it) => it.ToItemModel()).ToList();
			LocalSettings.Settings.ExtraSettings.DefaultSummaryTemplate = JsonConvert.SerializeObject(summaryTemplate);
		}
		else if (sync)
		{
			SummaryTemplates summaryTemplates = LocalSettings.Settings.UserPreference.summaryTemplates;
			SummaryTemplate summaryTemplate2 = summaryTemplates?.templates?.FirstOrDefault((SummaryTemplate it) => it.id == SelectedTemplateId);
			if (summaryTemplate2 != null)
			{
				handler(summaryTemplate2);
				summaryTemplates.mtime = Utils.GetNowTimeStampInMills();
				await SettingsHelper.PushLocalPreference();
			}
		}
	}

	private SummaryTemplate GetSelectedTemplate()
	{
		if (!string.IsNullOrEmpty(SelectedTemplateId))
		{
			SummaryTemplate summaryTemplate = SummaryTemplates.FirstOrDefault((SummaryTemplate it) => it.id == SelectedTemplateId);
			if (summaryTemplate != null)
			{
				return summaryTemplate;
			}
		}
		return SummaryTemplates.First();
	}

	private SummaryTemplate GetSelectedTemplate(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			SummaryTemplate summaryTemplate = SummaryTemplates.FirstOrDefault((SummaryTemplate it) => it.id == id);
			if (summaryTemplate != null)
			{
				return summaryTemplate;
			}
		}
		return SummaryTemplates.First();
	}

	public void NotifyProItems()
	{
		OnPropertyChanged("ProItemsText");
	}

	private string GetProItemsText()
	{
		if (UserDao.IsPro())
		{
			return string.Empty;
		}
		List<string> list = (from it in DisplayItems
			where it.Enabled && it.IsProItem
			select it.Name).ToList();
		if (list.Count > 0)
		{
			string text = "";
			if (Utils.IsEn())
			{
				text = " ";
			}
			return Utils.GetString("use_pro_summary_style") + text + string.Join(", ", list);
		}
		return string.Empty;
	}

	private static bool HasShareList()
	{
		return CacheManager.GetProjects().Any((ProjectModel it) => it.IsShareList());
	}

	private bool IsProjectCategoryAll()
	{
		if (_selectedProjectIds.IsNullOrEmpty())
		{
			return _selectedProjectGroupIds.IsNullOrEmpty();
		}
		return false;
	}

	private bool IsNonProjectRuleAll()
	{
		if (IsStatusAll() && IsPriorityAll() && IsTagAll())
		{
			return IsAssigneeAll();
		}
		return false;
	}

	private bool IsStatusAll()
	{
		if (_selectedStatus.Count != 0)
		{
			return _selectedStatus.Count == 4;
		}
		return true;
	}

	private bool IsAssigneeAll()
	{
		return Assignees.Count == 0;
	}

	private bool IsTagAll()
	{
		return _selectedTags.Count == 0;
	}

	private bool IsPriorityAll()
	{
		if (_selectedPriorities.Count != 0)
		{
			return _selectedPriorities.Count == 4;
		}
		return true;
	}

	public static string Filter2Rule(DateFilter filter, DateTime? start = null, DateTime? end = null)
	{
		string result = "thisweek";
		switch (filter)
		{
		case DateFilter.Today:
			result = "today";
			break;
		case DateFilter.Tomorrow:
			result = "tomorrow";
			break;
		case DateFilter.Yesterday:
			result = "offset(-1D)";
			break;
		case DateFilter.ThisWeek:
			result = "thisweek";
			break;
		case DateFilter.LastWeek:
			result = "offset(-1W)";
			break;
		case DateFilter.NextWeek:
			result = "nextweek";
			break;
		case DateFilter.ThisMonth:
			result = "thismonth";
			break;
		case DateFilter.LastMonth:
			result = "offset(-1M)";
			break;
		}
		if (filter == DateFilter.Custom && start.HasValue && end.HasValue)
		{
			int num = (int)(start.Value.Date - DateTime.Today.Date).TotalDays;
			int num2 = (int)(end.Value.Date - DateTime.Today.Date).TotalDays;
			result = $"span({num}~{num2})";
		}
		return result;
	}

	private static DateFilter ParseDateFilter(string rule)
	{
		DateFilter result = rule switch
		{
			"today" => DateFilter.Today, 
			"tomorrow" => DateFilter.Tomorrow, 
			"offset(-1D)" => DateFilter.Yesterday, 
			"thisweek" => DateFilter.ThisWeek, 
			"offset(-1W)" => DateFilter.LastWeek, 
			"thismonth" => DateFilter.ThisMonth, 
			"nextweek" => DateFilter.NextWeek, 
			"offset(-1M)" => DateFilter.LastMonth, 
			_ => DateFilter.ThisWeek, 
		};
		if (rule.Contains("span"))
		{
			result = DateFilter.Custom;
		}
		return result;
	}

	public void RefreshDate(string customRule = "")
	{
		if (DateFilter != DateFilter.Custom)
		{
			(DateTime, DateTime) tuple = SearchDateTextConverter.ConvertDateFilter2Span(DateFilter);
			StartDate = tuple.Item1;
			EndDate = tuple.Item2;
		}
		else if (!string.IsNullOrEmpty(customRule))
		{
			List<FilterDatePair> filterDatePairs = FilterUtils.GetFilterDatePairs(new List<string> { customRule });
			if (filterDatePairs != null && filterDatePairs.Count > 0)
			{
				FilterDatePair filterDatePair = filterDatePairs.FirstOrDefault();
				if (filterDatePair != null && filterDatePair.Start.HasValue && filterDatePair.End.HasValue)
				{
					StartDate = filterDatePair.Start.Value;
					EndDate = filterDatePair.End.Value.AddDays(-1.0);
				}
			}
		}
		else
		{
			StartDate = StartDate ?? DateTime.Today;
			EndDate = (EndDate ?? StartDate).Value;
		}
	}

	public void LoadTemplate()
	{
		SummaryTemplate summaryTemplate = SummaryTemplates.FirstOrDefault((SummaryTemplate it) => it.id == SelectedTemplateId);
		if (summaryTemplate == null)
		{
			return;
		}
		if (summaryTemplate.displayItems != null)
		{
			DisplayItems = new ObservableCollection<SummaryDisplayItemViewModel>(summaryTemplate.displayItems.Select((SummaryDisplayItem it) => new SummaryDisplayItemViewModel(it)));
		}
		_sortBy = TryParseSortType(summaryTemplate.sortType);
		_filter = Parser.ToNormalModel(summaryTemplate.rule);
		DateFilter = DateFilter.ThisWeek;
		string text = "";
		if (_filter.DueDates != null && _filter.DueDates.Count > 0)
		{
			text = _filter.DueDates[0];
			DateFilter = ParseDateFilter(text);
		}
		RefreshDate(text);
		SelectedPriorities = _filter.Priorities.Select((int it) => it.ToString()).ToList();
		SelectedTags = _filter.Tags.ToList();
		SelectedStatus = _filter.Status;
		SelectedProjectIds = _filter.Projects.ToList();
		SelectedProjectGroupIds = _filter.Groups.ToList();
		SelectedCalendarAccountIds = _filter.CalendarAccounts.ToList();
		SelectedCalendarIds = _filter.Calendars.ToList();
		SelectedProjectDisplayText = ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(new ProjectExtra
		{
			ProjectIds = _selectedProjectIds.Where((string id) => id != "Calendar5959a2259161d16d23a4f272").ToList(),
			GroupIds = _selectedProjectGroupIds.ToList(),
			BindAccounts = _selectedCalendarAccountIds.ToList(),
			SubscribeCalendars = ((_selectedCalendarIds.Any() || _selectedCalendarAccountIds.Any() || !_selectedProjectIds.Contains("Calendar5959a2259161d16d23a4f272")) ? _selectedCalendarIds.ToList() : new List<string> { "#allSubscribe" })
		}));
		SelectedTagDisplayText = TagCardViewModel.ToNormalDisplayText(_selectedTags);
		SelectedPriorityDisplayText = TaskPriorityTypeEditDialog.FormatDisplayText(_selectedPriorities);
		SelectedStatusDisplayText = TaskStatusTypeEditDialog.FormatDisplayText(_selectedStatus);
		Assignees = _filter.Assignees?.ToList() ?? new List<string>();
		SetAssignDisplayText(_filter.Assignees);
		List<string> list = new List<string>();
		if (_filter.Priorities.Count == 0)
		{
			list.Add("priority");
		}
		if (_filter.Assignees.Count == 0 && HasShareList())
		{
			list.Add("assignee");
		}
		MoreConditions = list;
	}

	public static void TryInitDefaultTemplate()
	{
		if (!LocalSettings.Settings.ExtraSettings.SummaryFilterImported)
		{
			SummaryFilterModel summaryFilter = LocalSettings.Settings.SummaryFilter;
			List<string> source = summaryFilter.SelectedProjectIds?.ToList() ?? new List<string>();
			if (summaryFilter.SelectedProjectIds.Count == 0 && summaryFilter.SelectedProjectGroupIds.Count == 0)
			{
				source = new List<string> { "ProjectAll2e4c103c57ef480997943206" };
			}
			NormalFilterViewModel normalFilterViewModel = new NormalFilterViewModel
			{
				Projects = source.ToList(),
				Groups = (summaryFilter.SelectedProjectGroupIds?.ToList() ?? new List<string>()),
				Tags = (summaryFilter.SelectedTags?.ToList() ?? new List<string>()),
				Priorities = (summaryFilter.SelectedPriorities?.Select(int.Parse).ToList() ?? new List<int>()),
				Assignees = (summaryFilter.Assigns?.ToList() ?? new List<string>()),
				DueDates = new List<string> { Filter2Rule(summaryFilter.DateFilter, summaryFilter.StartDate, summaryFilter.EndDate) },
				CompletedTimes = new List<string> { Filter2Rule(summaryFilter.DateFilter, summaryFilter.StartDate, summaryFilter.EndDate) },
				Version = 6
			};
			List<SummaryDisplayItemModel> source2 = InitDefaultDisplayItems();
			source2.First((SummaryDisplayItemModel it) => it.Key == "detail").Enabled = summaryFilter.ShowDetail;
			source2.First((SummaryDisplayItemModel it) => it.Key == "focus").Enabled = summaryFilter.ShowPomo;
			source2.First((SummaryDisplayItemModel it) => it.Key == "progress").Enabled = summaryFilter.ShowProgress;
			source2.First((SummaryDisplayItemModel it) => it.Key == "project").Enabled = summaryFilter.ShowBelongList;
			source2.First((SummaryDisplayItemModel it) => it.Key == "completedTime").Enabled = summaryFilter.ShowCompleteDate;
			SummaryTemplate value = new SummaryTemplate
			{
				id = "defaultId",
				name = Utils.GetString("Summary"),
				rule = normalFilterViewModel.ToRule(isSave: false),
				sortType = summaryFilter.SortBy.ToString(),
				displayItems = source2.Select((SummaryDisplayItemModel it) => new SummaryDisplayItem
				{
					key = it.Key,
					sortOrder = it.SortOrder,
					style = it.Style,
					enabled = it.Enabled
				}).ToList()
			};
			LocalSettings.Settings.ExtraSettings.SummaryFilterImported = true;
			LocalSettings.Settings.ExtraSettings.DefaultSummaryTemplate = JsonConvert.SerializeObject(value);
			LocalSettings.Settings.Save();
		}
	}

	public void SetModel(SummaryFilterModel model)
	{
		if (model == null)
		{
			return;
		}
		DateFilter = model.DateFilter;
		_selectedTags = model.SelectedTags?.ToList() ?? _selectedTags;
		_selectedProjectIds = model.SelectedProjectIds?.ToList() ?? _selectedProjectIds;
		_selectedProjectGroupIds = model.SelectedProjectGroupIds?.ToList() ?? _selectedProjectGroupIds;
		_selectedStatus = model.SelectedStatus ?? _selectedStatus;
		_selectedPriorities = model.SelectedPriorities?.ToList() ?? _selectedPriorities;
		StartDate = model.StartDate ?? _startDate;
		EndDate = model.EndDate ?? _endDate;
		Assignees = model.Assigns?.ToList() ?? new List<string>();
		_sortBy = model.SortBy;
		_showCompleteDate = model.ShowCompleteDate;
		_showDetail = model.ShowDetail;
		_showPomo = model.ShowPomo;
		_showProgress = model.ShowProgress;
		_showBelongList = model.ShowBelongList;
		SelectedProjectDisplayText = ProjectExtra.FormatDisplayText(_selectedProjectIds, _selectedProjectGroupIds);
		SelectedTagDisplayText = TagCardViewModel.ToNormalDisplayText(_selectedTags);
		SelectedPriorityDisplayText = TaskPriorityTypeEditDialog.FormatDisplayText(_selectedPriorities);
		SelectedStatusDisplayText = TaskStatusTypeEditDialog.FormatDisplayText(_selectedStatus);
		SetAssignDisplayText(Assignees);
		if (!model.DisplayItems.IsNullOrEmpty())
		{
			DisplayItems = new ObservableCollection<SummaryDisplayItemViewModel>((from item in model.DisplayItems
				select new SummaryDisplayItemViewModel(item) into it
				orderby it.SortOrder
				select it).ToList());
		}
		else
		{
			DisplayItems = new ObservableCollection<SummaryDisplayItemViewModel>((from m in InitDefaultDisplayItems()
				select new SummaryDisplayItemViewModel(m)).ToList());
		}
	}

	public void SetupDefaultItems()
	{
		DisplayItems = new ObservableCollection<SummaryDisplayItemViewModel>((from m in InitDefaultDisplayItems()
			select new SummaryDisplayItemViewModel(m)).ToList());
	}

	public static List<SummaryDisplayItemModel> InitDefaultDisplayItems()
	{
		return new List<SummaryDisplayItemModel>
		{
			new SummaryDisplayItemModel("status", 0L, "", enabled: false),
			new SummaryDisplayItemModel("progress", 1L, "", enabled: true),
			new SummaryDisplayItemModel("completedTime", 2L, "date", enabled: true),
			new SummaryDisplayItemModel("dueDate", 3L, "date", enabled: false),
			new SummaryDisplayItemModel("title", 4L, "", enabled: true),
			new SummaryDisplayItemModel("parentTask", 5L, "", enabled: false),
			new SummaryDisplayItemModel("tag", 6L, "", enabled: false),
			new SummaryDisplayItemModel("focus", 7L, "", enabled: false),
			new SummaryDisplayItemModel("project", 8L, "project", enabled: false),
			new SummaryDisplayItemModel("detail", 9L, "text", enabled: false)
		};
	}

	public void Save()
	{
		ToFilterModel().Save();
	}

	public SummaryFilterModel ToFilterModel()
	{
		return new SummaryFilterModel
		{
			DateFilter = _dateFilter,
			SelectedTags = _selectedTags,
			SelectedProjectIds = _selectedProjectIds,
			SelectedProjectGroupIds = _selectedProjectGroupIds,
			SelectedPriorities = _selectedPriorities,
			SelectedStatus = _selectedStatus,
			Assigns = Assignees,
			StartDate = _startDate,
			EndDate = _endDate,
			SortBy = _sortBy,
			ShowCompleteDate = _showCompleteDate,
			ShowBelongList = _showBelongList,
			ShowDetail = _showDetail,
			ShowPomo = _showPomo,
			ShowProgress = _showProgress,
			DisplayItems = DisplayItems.Select((SummaryDisplayItemViewModel vm) => vm.ToModel()).ToList()
		};
	}

	private string GetStyleByKey(string key)
	{
		SummaryDisplayItemViewModel summaryDisplayItemViewModel = DisplayItems.FirstOrDefault((SummaryDisplayItemViewModel t) => t.Key == key);
		if (summaryDisplayItemViewModel != null)
		{
			return summaryDisplayItemViewModel.Style ?? summaryDisplayItemViewModel.SupportedStyles.FirstOrDefault();
		}
		return null;
	}

	private bool IsParentBeforeTitle()
	{
		SummaryDisplayItemViewModel summaryDisplayItemViewModel = DisplayItems.FirstOrDefault((SummaryDisplayItemViewModel t) => t.Key == "parentTask");
		SummaryDisplayItemViewModel summaryDisplayItemViewModel2 = DisplayItems.FirstOrDefault((SummaryDisplayItemViewModel t) => t.Key == "title");
		if (summaryDisplayItemViewModel != null && summaryDisplayItemViewModel2 != null)
		{
			return summaryDisplayItemViewModel.SortOrder < summaryDisplayItemViewModel2.SortOrder;
		}
		return true;
	}

	public bool ShowClearSummaryOption()
	{
		if (DateFilter != DateFilter.ThisWeek)
		{
			return true;
		}
		if (SelectedProjectIds.Count != 1 || !CalendarUtils.IsAllListId(SelectedProjectIds[0]))
		{
			return true;
		}
		if (SelectedProjectGroupIds.Count > 0)
		{
			return true;
		}
		if (SelectedPriorities.Count > 0)
		{
			return true;
		}
		if (SelectedTags.Count > 0)
		{
			return true;
		}
		if (SelectedStatus.Count > 0)
		{
			return true;
		}
		if (Assignees.Count > 0)
		{
			return true;
		}
		return false;
	}

	private bool IsDisplayItemEnabled(string key)
	{
		return DisplayItems.FirstOrDefault((SummaryDisplayItemViewModel t) => t.Key == key)?.Enabled ?? false;
	}

	public void SetAssignDisplayText(List<string> assignees)
	{
		if (assignees == null || assignees.Count == 0)
		{
			SelectedAssignTypeText = Utils.GetString("AssignTo");
			return;
		}
		string assigneeValueText = AssigneeCardViewModel.GetAssigneeValueText(assignees);
		SelectedAssignTypeText = assigneeValueText;
	}

	public async void DeleteTemplate(string modelId)
	{
		SummaryTemplate summaryTemplate = SummaryTemplates.FirstOrDefault((SummaryTemplate it) => it.id == modelId);
		if (summaryTemplate != null)
		{
			SummaryTemplates.Remove(summaryTemplate);
			SummaryTemplates summaryTemplates = LocalSettings.Settings.UserPreference.summaryTemplates;
			SummaryTemplate summaryTemplate2 = summaryTemplates?.templates?.FirstOrDefault((SummaryTemplate it) => it.id == modelId);
			if (summaryTemplate2 != null)
			{
				summaryTemplates.templates.Remove(summaryTemplate2);
				summaryTemplates.mtime = Utils.GetNowTimeStampInMills();
				LocalSettings.Settings.UserPreference.summaryTemplates = summaryTemplates;
				LocalSettings.Settings.Save();
				await SettingsHelper.PushLocalPreference();
			}
		}
	}

	public void RemoveProItems()
	{
		foreach (SummaryDisplayItemViewModel displayItem in DisplayItems)
		{
			if (displayItem.IsProItem && displayItem.Enabled)
			{
				displayItem.Enabled = false;
			}
		}
		NotifyProItems();
	}
}
