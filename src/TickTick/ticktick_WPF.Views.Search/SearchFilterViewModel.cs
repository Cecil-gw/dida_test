using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.Views.Search;

public class SearchFilterViewModel : BaseViewModel
{
	private DateFilter _dateFilter;

	private DateTime? _endDate;

	private ProjectOrGroupFilter _projectOrGroupFilter;

	private string _searchKey = string.Empty;

	private string _selectedProjectDisplayText = Utils.GetString("AllList");

	private List<string> _selectedProjectGroupIds = new List<string>();

	private List<string> _selectedProjectIds = new List<string>();

	private List<string> _selectedCalendarAccountIds = new List<string>();

	private List<string> _selectedCalendarIds = new List<string>();

	private string _selectedTagDisplayText = Utils.GetString("AllTags");

	private List<int> _selectedPriorities = new List<int>();

	private List<string> _selectedAssignees = new List<string>();

	private List<string> _selectedTags = new List<string>();

	private List<string> _searchTags = new List<string>();

	private DateTime? _startDate;

	private StatusFilter _statusFilter;

	private TaskType _selectedType = TaskType.TaskAndNote;

	private SearchTypeFilter _selectedSearchType;

	private SearchKind _kind;

	private SearchCategory _category;

	private bool _showPriorityFilter;

	private bool _showAssignFilter;

	private bool _showTaskTypeFilter;

	private bool _showTaskContentTypeFilter;

	private bool _showDateFilter = true;

	private bool _showListFilter = true;

	private bool _showTagFilter = true;

	private bool _showStatusFilter = true;

	private bool _assignFilterEnable = true;

	private bool _taskTypeFilterEnable = true;

	public SearchKind Kind
	{
		get
		{
			return _kind;
		}
		set
		{
			_kind = value;
		}
	}

	public SearchCategory Category
	{
		get
		{
			return _category;
		}
		set
		{
			_category = value;
			OnPropertyChanged("Category");
			NotifyFilterStateChanged();
		}
	}

	public SearchTypeFilter SelectedSearchType
	{
		get
		{
			return _selectedSearchType;
		}
		set
		{
			_selectedSearchType = value;
			OnPropertyChanged("SelectedSearchType");
			NotifyFilterStateChanged();
		}
	}

	public bool ShowMoreFilter
	{
		get
		{
			if (Category != SearchCategory.CalendarEvent && (Category == SearchCategory.None || Category == SearchCategory.Task))
			{
				if (ShowPriorityFilter)
				{
					if (AssignFilterEnable)
					{
						return !ShowAssignFilter;
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public bool ShowDateFilter
	{
		get
		{
			return _showDateFilter;
		}
		set
		{
			_showDateFilter = value;
			OnPropertyChanged("ShowDateFilter");
		}
	}

	public bool ShowListFilter
	{
		get
		{
			return _showListFilter;
		}
		set
		{
			_showListFilter = value;
			OnPropertyChanged("ShowListFilter");
		}
	}

	public bool ShowTagFilter
	{
		get
		{
			return _showTagFilter;
		}
		set
		{
			_showTagFilter = value;
			OnPropertyChanged("ShowTagFilter");
		}
	}

	public bool ShowStatusFilter
	{
		get
		{
			return _showStatusFilter;
		}
		set
		{
			_showStatusFilter = value;
			OnPropertyChanged("ShowStatusFilter");
		}
	}

	public bool ShowPriorityFilter
	{
		get
		{
			return _showPriorityFilter;
		}
		set
		{
			_showPriorityFilter = value;
			OnPropertyChanged("ShowPriorityFilter");
			OnPropertyChanged("ShowMoreFilter");
		}
	}

	public bool ShowAssignFilter
	{
		get
		{
			return _showAssignFilter;
		}
		set
		{
			_showAssignFilter = value;
			OnPropertyChanged("ShowAssignFilter");
			OnPropertyChanged("ShowMoreFilter");
		}
	}

	public bool ShowTaskTypeFilter
	{
		get
		{
			return _showTaskTypeFilter;
		}
		set
		{
			_showTaskTypeFilter = value;
			OnPropertyChanged("ShowTaskTypeFilter");
			OnPropertyChanged("ShowMoreFilter");
		}
	}

	public bool ShowTaskContentTypeFilter
	{
		get
		{
			return _showTaskContentTypeFilter;
		}
		set
		{
			_showTaskContentTypeFilter = value;
			OnPropertyChanged("ShowTaskContentTypeFilter");
			OnPropertyChanged("ShowMoreFilter");
		}
	}

	public bool AssignFilterEnable
	{
		get
		{
			return _assignFilterEnable;
		}
		set
		{
			_assignFilterEnable = value;
			OnPropertyChanged("AssignFilterEnable");
			OnPropertyChanged("ShowMoreFilter");
		}
	}

	public bool TaskTypeFilterEnable
	{
		get
		{
			return _taskTypeFilterEnable;
		}
		set
		{
			_taskTypeFilterEnable = value;
			OnPropertyChanged("TaskTypeFilterEnable");
			OnPropertyChanged("ShowMoreFilter");
		}
	}

	public List<string> SearchTags
	{
		get
		{
			return _searchTags;
		}
		set
		{
			_searchTags = value;
			OnPropertyChanged("SearchTags");
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
			NotifyFilterStateChanged();
		}
	}

	public List<int> SelectedPriorities
	{
		get
		{
			return _selectedPriorities;
		}
		set
		{
			_selectedPriorities = value;
			OnPropertyChanged("SelectedPriorities");
			NotifyFilterStateChanged();
		}
	}

	public List<string> SelectedAssignees
	{
		get
		{
			return _selectedAssignees;
		}
		set
		{
			_selectedAssignees = value;
			OnPropertyChanged("SelectedAssignees");
			NotifyFilterStateChanged();
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
			NotifyFilterStateChanged();
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
			NotifyFilterStateChanged();
		}
	}

	public List<string> SelectedCalendarAccountIds
	{
		get
		{
			return _selectedCalendarAccountIds;
		}
		set
		{
			_selectedCalendarAccountIds = value;
			OnPropertyChanged("SelectedCalendarAccountIds");
			NotifyFilterStateChanged();
		}
	}

	public List<string> SelectedCalendarIds
	{
		get
		{
			return _selectedCalendarIds;
		}
		set
		{
			_selectedCalendarIds = value;
			OnPropertyChanged("SelectedCalendarIds");
			NotifyFilterStateChanged();
		}
	}

	public TaskType SelectedType
	{
		get
		{
			return _selectedType;
		}
		set
		{
			_selectedType = value;
			OnPropertyChanged("SelectedType");
			NotifyFilterStateChanged();
		}
	}

	public ProjectOrGroupFilter ProjectOrGroupFilter
	{
		get
		{
			return _projectOrGroupFilter;
		}
		set
		{
			_projectOrGroupFilter = value;
			OnPropertyChanged("ProjectOrGroupFilter");
			NotifyFilterStateChanged();
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
			NotifyFilterStateChanged();
		}
	}

	public StatusFilter StatusFilter
	{
		get
		{
			return _statusFilter;
		}
		set
		{
			_statusFilter = value;
			OnPropertyChanged("StatusFilter");
			NotifyFilterStateChanged();
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
			NotifyFilterStateChanged();
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
			NotifyFilterStateChanged();
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
			NotifyFilterStateChanged();
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
			NotifyFilterStateChanged();
		}
	}

	public string SearchKey
	{
		get
		{
			return _searchKey;
		}
		set
		{
			_searchKey = value;
			OnPropertyChanged("SearchKey");
		}
	}

	public bool Searched { get; set; }

	public bool HasTypeFilter => SelectedSearchType != SearchTypeFilter.All;

	public bool HasTaskContentTypeFilter => SelectedType != TaskType.TaskAndNote;

	public bool HasProjectOrGroupFilter
	{
		get
		{
			if (ProjectOrGroupFilter != ProjectOrGroupFilter.Custom && (SelectedProjectIds == null || SelectedProjectIds.Count <= 0) && (SelectedProjectGroupIds == null || SelectedProjectGroupIds.Count <= 0) && (SelectedCalendarAccountIds == null || SelectedCalendarAccountIds.Count <= 0))
			{
				if (SelectedCalendarIds != null)
				{
					return SelectedCalendarIds.Count > 0;
				}
				return false;
			}
			return true;
		}
	}

	public bool HasTagFilter
	{
		get
		{
			if (SelectedTags != null)
			{
				return SelectedTags.Count > 0;
			}
			return false;
		}
	}

	public bool HasDateFilter
	{
		get
		{
			if (DateFilter == DateFilter.All && !StartDate.HasValue)
			{
				return EndDate.HasValue;
			}
			return true;
		}
	}

	public bool HasStatusFilter => StatusFilter != StatusFilter.All;

	public bool HasPriorityFilter
	{
		get
		{
			if (SelectedPriorities != null)
			{
				return SelectedPriorities.Count > 0;
			}
			return false;
		}
	}

	public bool HasAssignFilter
	{
		get
		{
			if (SelectedAssignees != null)
			{
				return SelectedAssignees.Count > 0;
			}
			return false;
		}
	}

	public void NotifyFilterStateChanged()
	{
		OnPropertyChanged("HasTypeFilter");
		OnPropertyChanged("HasTaskContentTypeFilter");
		OnPropertyChanged("HasProjectOrGroupFilter");
		OnPropertyChanged("HasTagFilter");
		OnPropertyChanged("HasDateFilter");
		OnPropertyChanged("HasStatusFilter");
		OnPropertyChanged("HasPriorityFilter");
		OnPropertyChanged("HasAssignFilter");
		OnPropertyChanged("ShowMoreFilter");
	}

	public void ApplySearchType(SearchTypeFilter type)
	{
		SelectedSearchType = type;
		switch (type)
		{
		case SearchTypeFilter.Task:
			Category = SearchCategory.Task;
			SelectedType = TaskType.TaskAndNote;
			break;
		case SearchTypeFilter.Tag:
			Category = SearchCategory.Tag;
			SelectedType = TaskType.TaskAndNote;
			break;
		case SearchTypeFilter.List:
			Category = SearchCategory.List;
			SelectedType = TaskType.TaskAndNote;
			break;
		case SearchTypeFilter.Filter:
			Category = SearchCategory.Filter;
			SelectedType = TaskType.TaskAndNote;
			break;
		case SearchTypeFilter.CalendarEvent:
			Category = SearchCategory.CalendarEvent;
			SelectedType = TaskType.TaskAndNote;
			break;
		default:
			Category = SearchCategory.None;
			SelectedType = TaskType.TaskAndNote;
			break;
		}
		RefreshCategoryState();
	}

	public void ResetAllFilterConditions()
	{
		Category = SearchCategory.None;
		SelectedSearchType = SearchTypeFilter.All;
		SelectedType = TaskType.TaskAndNote;
		ProjectOrGroupFilter = ProjectOrGroupFilter.All;
		SelectedProjectIds = new List<string>();
		SelectedProjectGroupIds = new List<string>();
		SelectedCalendarAccountIds = new List<string>();
		SelectedCalendarIds = new List<string>();
		SelectedTags = new List<string>();
		SelectedTagDisplayText = Utils.GetString("AllTags");
		DateFilter = DateFilter.All;
		StartDate = null;
		EndDate = null;
		StatusFilter = StatusFilter.All;
		SelectedPriorities = new List<int>();
		SelectedAssignees = new List<string>();
		SelectedProjectDisplayText = GetDefaultProjectDisplayText();
		RefreshCategoryState();
	}

	public void ApplySearchFilter(SearchFilterModel model)
	{
		if (model == null)
		{
			ResetAllFilterConditions();
			return;
		}
		Category = model.Category;
		SearchKey = model.Key;
		Kind = model.Kind;
		DateFilter = model.DateFilter;
		StartDate = model.Start;
		EndDate = ((model.DateFilter == DateFilter.Custom && model.End.HasValue) ? new DateTime?(model.End.Value.AddDays(-1.0)) : model.End);
		SelectedType = model.TaskType;
		SelectedProjectIds = ((model.ProjectIds != null) ? new List<string>(model.ProjectIds) : new List<string>());
		SelectedProjectGroupIds = ((model.GroupIds != null) ? new List<string>(model.GroupIds) : new List<string>());
		SelectedCalendarAccountIds = ((model.CalendarAccountIds != null) ? new List<string>(model.CalendarAccountIds) : new List<string>());
		SelectedCalendarIds = ((model.CalendarIds != null) ? new List<string>(model.CalendarIds) : new List<string>());
		SelectedTags = ((model.Tags != null) ? new List<string>(model.Tags) : new List<string>());
		SelectedPriorities = ((model.Priorities != null) ? new List<int>(model.Priorities) : new List<int>());
		SelectedAssignees = ((model.Assignees != null) ? new List<string>(model.Assignees) : new List<string>());
		ProjectOrGroupFilter = ((SelectedProjectIds.Count > 0 || SelectedProjectGroupIds.Count > 0 || SelectedCalendarAccountIds.Count > 0 || SelectedCalendarIds.Count > 0) ? ProjectOrGroupFilter.Custom : ProjectOrGroupFilter.All);
		if (model.Status == 2)
		{
			StatusFilter = StatusFilter.Completed;
		}
		else if (model.Status == 0)
		{
			StatusFilter = StatusFilter.Uncompleted;
		}
		else if (model.Status == -1)
		{
			StatusFilter = StatusFilter.Abandoned;
		}
		else
		{
			StatusFilter = StatusFilter.All;
		}
		SelectedTagDisplayText = ((SelectedTags.Count > 0) ? TagCardViewModel.ToNormalDisplayText(SelectedTags) : Utils.GetString("AllTags"));
		SelectedProjectDisplayText = ((ProjectOrGroupFilter == ProjectOrGroupFilter.Custom) ? ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(new ProjectExtra
		{
			ProjectIds = SelectedProjectIds.ToList(),
			GroupIds = SelectedProjectGroupIds.ToList(),
			BindAccounts = SelectedCalendarAccountIds.ToList(),
			SubscribeCalendars = SelectedCalendarIds.ToList()
		})) : GetDefaultProjectDisplayText());
		RefreshCategoryState();
	}

	public void SyncSearchTypeFromCategory()
	{
		if (Category == SearchCategory.CalendarEvent)
		{
			SelectedSearchType = SearchTypeFilter.CalendarEvent;
		}
		else if (Category == SearchCategory.Tag)
		{
			SelectedSearchType = SearchTypeFilter.Tag;
		}
		else if (Category == SearchCategory.List)
		{
			SelectedSearchType = SearchTypeFilter.List;
		}
		else if (Category == SearchCategory.Filter)
		{
			SelectedSearchType = SearchTypeFilter.Filter;
		}
		else if (Category == SearchCategory.Task)
		{
			SelectedSearchType = SearchTypeFilter.Task;
		}
		else
		{
			SelectedSearchType = SearchTypeFilter.All;
		}
	}

	public void RefreshCategoryState()
	{
		ShowTaskTypeFilter = false;
		ShowTaskContentTypeFilter = false;
		ShowDateFilter = true;
		ShowListFilter = true;
		ShowTagFilter = Category != SearchCategory.CalendarEvent;
		ShowStatusFilter = Category != SearchCategory.CalendarEvent;
		ShowPriorityFilter = Category == SearchCategory.None || Category == SearchCategory.Task;
		ShowAssignFilter = (Category == SearchCategory.None || Category == SearchCategory.Task) && AssignFilterEnable;
		if (Category == SearchCategory.CalendarEvent)
		{
			SelectedTags = new List<string>();
			SelectedTagDisplayText = Utils.GetString("AllTags");
			SelectedPriorities = new List<int>();
			SelectedAssignees = new List<string>();
			StatusFilter = StatusFilter.All;
			SelectedType = TaskType.TaskAndNote;
			ShowPriorityFilter = false;
			ShowAssignFilter = false;
			SelectedProjectIds = new List<string>();
			SelectedProjectGroupIds = new List<string>();
		}
		else if (Category == SearchCategory.Tag || Category == SearchCategory.List || Category == SearchCategory.Filter)
		{
			SelectedTags = new List<string>();
			SelectedTagDisplayText = Utils.GetString("AllTags");
			SelectedPriorities = new List<int>();
			SelectedAssignees = new List<string>();
			StatusFilter = StatusFilter.All;
			DateFilter = DateFilter.All;
			StartDate = null;
			EndDate = null;
			SelectedProjectIds = new List<string>();
			SelectedProjectGroupIds = new List<string>();
			SelectedCalendarAccountIds = new List<string>();
			SelectedCalendarIds = new List<string>();
			ShowDateFilter = false;
			ShowListFilter = false;
			ShowTagFilter = false;
			ShowStatusFilter = false;
			ShowPriorityFilter = false;
			ShowAssignFilter = false;
		}
		else if (Category == SearchCategory.Task)
		{
			SelectedCalendarAccountIds = new List<string>();
			SelectedCalendarIds = new List<string>();
		}
		if (ProjectOrGroupFilter == ProjectOrGroupFilter.All)
		{
			SelectedProjectDisplayText = GetDefaultProjectDisplayText();
		}
		SyncSearchTypeFromCategory();
		OnPropertyChanged("ShowMoreFilter");
		NotifyFilterStateChanged();
	}

	public string GetDefaultProjectDisplayText()
	{
		return Category switch
		{
			SearchCategory.CalendarEvent => Utils.GetString("AllCalendars"), 
			SearchCategory.Task => Utils.GetString("AllList"), 
			_ => Utils.GetString("All"), 
		};
	}

	public SearchFilterModel ToSearchFilter()
	{
		SearchFilterModel searchFilterModel = new SearchFilterModel();
		searchFilterModel.DateFilter = _dateFilter;
		switch (_dateFilter)
		{
		case DateFilter.ThisWeek:
			searchFilterModel.Start = Utils.GetWeekStart(DateTime.Today);
			searchFilterModel.End = searchFilterModel.Start.Value.AddDays(7.0);
			break;
		case DateFilter.NextWeek:
			searchFilterModel.Start = Utils.GetWeekStart(DateTime.Today).AddDays(7.0);
			searchFilterModel.End = searchFilterModel.Start.Value.AddDays(7.0);
			break;
		case DateFilter.ThisMonth:
		{
			int day = DateTime.Now.Date.Day;
			searchFilterModel.Start = DateTime.Now.Date.AddDays(1 - day);
			searchFilterModel.End = searchFilterModel.Start.Value.AddMonths(1);
			break;
		}
		case DateFilter.LastWeek:
			searchFilterModel.Start = Utils.GetWeekStart(DateTime.Today).AddDays(-7.0);
			searchFilterModel.End = searchFilterModel.Start.Value.AddDays(7.0);
			break;
		case DateFilter.LastMonth:
		{
			int day2 = DateTime.Now.Date.Day;
			searchFilterModel.Start = DateTime.Now.Date.AddDays(1 - day2).AddMonths(-1);
			searchFilterModel.End = searchFilterModel.Start.Value.AddMonths(1);
			break;
		}
		case DateFilter.Custom:
		{
			searchFilterModel.Start = StartDate;
			DateTime? endDate = EndDate;
			searchFilterModel.End = (endDate.HasValue ? new DateTime?(endDate.GetValueOrDefault().AddDays(1.0)) : EndDate);
			break;
		}
		}
		switch (StatusFilter)
		{
		case StatusFilter.Completed:
			searchFilterModel.Status = 2;
			break;
		case StatusFilter.Uncompleted:
			searchFilterModel.Status = 0;
			break;
		case StatusFilter.Abandoned:
			searchFilterModel.Status = -1;
			break;
		}
		if (ProjectOrGroupFilter == ProjectOrGroupFilter.Custom)
		{
			searchFilterModel.ProjectIds = SelectedProjectIds.ToList();
			if (SelectedProjectGroupIds.Count > 0)
			{
				foreach (string selectedProjectGroupId in SelectedProjectGroupIds)
				{
					List<ProjectModel> projectsInGroup = CacheManager.GetProjectsInGroup(selectedProjectGroupId);
					if (projectsInGroup.Count > 0)
					{
						searchFilterModel.ProjectIds.AddRange(projectsInGroup.Select((ProjectModel p) => p.id));
					}
				}
			}
		}
		searchFilterModel.Tags = _selectedTags.ToList();
		searchFilterModel.Priorities = _selectedPriorities.ToList();
		searchFilterModel.Assignees = _selectedAssignees.ToList();
		searchFilterModel.TaskType = _selectedType;
		searchFilterModel.GroupIds = _selectedProjectGroupIds.ToList();
		searchFilterModel.CalendarAccountIds = _selectedCalendarAccountIds.ToList();
		searchFilterModel.CalendarIds = _selectedCalendarIds.ToList();
		searchFilterModel.Key = _searchKey;
		searchFilterModel.Kind = _kind;
		searchFilterModel.Category = _category;
		return searchFilterModel;
	}
}
