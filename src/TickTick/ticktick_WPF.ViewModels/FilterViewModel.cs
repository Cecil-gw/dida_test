using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.ViewModels;

public class FilterViewModel : BaseViewModel
{
	private AdvancedFilterViewModel _advancedViewModel;

	private Constants.ModelEditType _editType;

	private FilterModel _filterModel;

	private FilterMode _mode;

	private string _name;

	private NormalFilterViewModel _normalViewModel;

	public NormalFilterViewModel NormalViewModel
	{
		get
		{
			return _normalViewModel;
		}
		set
		{
			_normalViewModel = value;
		}
	}

	public FilterMode Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			_mode = value;
			OnPropertyChanged("Mode");
		}
	}

	public AdvancedFilterViewModel AdvancedViewModel
	{
		get
		{
			return _advancedViewModel;
		}
		set
		{
			_advancedViewModel = value;
		}
	}

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			OnPropertyChanged("Name");
		}
	}

	public Constants.ModelEditType EditType
	{
		get
		{
			return _editType;
		}
		set
		{
			_editType = value;
			OnPropertyChanged("EditType");
		}
	}

	public FilterModel FilterModel
	{
		get
		{
			return _filterModel;
		}
		set
		{
			_filterModel = value;
		}
	}

	public FilterViewModel()
	{
		_name = string.Empty;
		_editType = Constants.ModelEditType.New;
		_filterModel = new FilterModel();
		_mode = FilterMode.Normal;
		_normalViewModel = new NormalFilterViewModel();
		_advancedViewModel = new AdvancedFilterViewModel();
	}

	public FilterViewModel(FilterModel filterModel)
	{
		_name = filterModel.name;
		_editType = ((!string.IsNullOrEmpty(filterModel.id)) ? Constants.ModelEditType.Edit : Constants.ModelEditType.New);
		_filterModel = filterModel.Copy();
		_mode = (FilterMode)Parser.GetFilterRuleType(filterModel.rule);
		if (_mode == FilterMode.Normal)
		{
			_normalViewModel = Parser.ToNormalModel(filterModel.rule);
			_advancedViewModel = new AdvancedFilterViewModel();
		}
		else
		{
			_normalViewModel = new NormalFilterViewModel();
			_advancedViewModel = Parser.ToAdvanceModel(filterModel.rule);
		}
	}

	public FilterViewModel(SearchFilterModel searchFilter)
	{
		_editType = Constants.ModelEditType.New;
		_filterModel = new FilterModel();
		_mode = FilterMode.Normal;
		_normalViewModel = new NormalFilterViewModel(searchFilter);
		_advancedViewModel = new AdvancedFilterViewModel();
	}

	public FilterModel GetFilterModel()
	{
		if (EditType == Constants.ModelEditType.New)
		{
			FilterModel filterModel = new FilterModel
			{
				name = _name,
				deleted = 0,
				syncStatus = 0,
				createdTime = DateTime.Now,
				modifiedTime = DateTime.Now,
				sortType = Constants.SortType.project.ToString()
			};
			SaveRule(filterModel);
			return filterModel;
		}
		_filterModel.name = _name;
		SaveRule(_filterModel);
		return _filterModel;
	}

	public async Task<FilterModel> Save()
	{
		if (EditType == Constants.ModelEditType.New)
		{
			FilterModel filterModel = new FilterModel
			{
				name = _name,
				deleted = 0,
				syncStatus = 0,
				id = Utils.GetGuid(),
				createdTime = DateTime.Now,
				modifiedTime = DateTime.Now,
				sortType = Constants.SortType.project.ToString()
			};
			FilterModel filterModel2 = filterModel;
			filterModel2.sortOrder = await FilterDao.GetNextFilterSortOrder();
			filterModel.SortOption = SortOptionUtils.GetSortOptionBySortType(Constants.SortType.project, inKanban: false);
			_filterModel = filterModel;
			SaveRule(_filterModel);
			await FilterDao.AddFilter(_filterModel);
		}
		else
		{
			_filterModel.name = _name;
			_filterModel.modifiedTime = DateTime.Now;
			SaveRule(_filterModel);
			if (_filterModel.syncStatus != 0)
			{
				_filterModel.syncStatus = 1;
			}
			await FilterSyncJsonDao.TrySaveFilter(_filterModel.id);
			await FilterDao.UpdateFilter(_filterModel);
		}
		return _filterModel;
	}

	private void SaveRule(FilterModel filter)
	{
		bool flag = false;
		if (_mode == FilterMode.Normal)
		{
			string text = _normalViewModel.ToRule(isSave: true);
			if (string.IsNullOrEmpty(text))
			{
				string text2 = _advancedViewModel.ToRule(isSave: true);
				filter.rule = ((!string.IsNullOrEmpty(text2)) ? text2 : text);
			}
			else
			{
				filter.rule = text;
				flag = true;
			}
		}
		else
		{
			filter.rule = _advancedViewModel.ToRule(isSave: true);
		}
		UserActCollectUtils.AddClickEvent("edit_filter", "mode", flag ? "normal" : "advance");
		if (flag ? _normalViewModel.OnlyNote() : _advancedViewModel.OnlyNote())
		{
			if (EditType == Constants.ModelEditType.New || filter.SortOption == null)
			{
				filter.SortOption = new SortOption
				{
					groupBy = "project",
					orderBy = "createdTime"
				};
			}
			else
			{
				if (filter.SortOption.groupByType == Constants.SortType.priority || filter.SortOption.groupByType == Constants.SortType.dueDate)
				{
					filter.SortOption.groupBy = "project";
				}
				if (filter.SortOption.orderByType == Constants.SortType.priority || filter.SortOption.orderByType == Constants.SortType.dueDate)
				{
					filter.SortOption.orderBy = "createdTime";
				}
			}
		}
		if (flag ? (!_normalViewModel.OnlyNote()) : (!_advancedViewModel.OnlyNote()))
		{
			if (EditType == Constants.ModelEditType.New || filter.SortOption == null)
			{
				filter.SortOption = new SortOption
				{
					groupBy = "project",
					orderBy = "dueDate"
				};
			}
			else if (filter.SortOption.orderByType == Constants.SortType.createdTime || filter.SortOption.orderByType == Constants.SortType.modifiedTime)
			{
				filter.SortOption.orderBy = "dueDate";
			}
		}
	}

	public static FilterTaskDefault CalculateTaskDefault(string filterRule, bool withEmptyProject = false)
	{
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		FilterTaskDefault filterTaskDefault = new FilterTaskDefault
		{
			Priority = defaultSafely.Priority,
			DefaultDate = defaultSafely.GetDefaultDateTime()
		};
		if (Parser.GetFilterRuleType(filterRule) == 0)
		{
			GetDefaultInNormal(Parser.ToNormalModel(filterRule), filterTaskDefault);
		}
		else
		{
			AdvancedFilterViewModel advancedFilterViewModel = Parser.ToAdvanceModel(filterRule);
			List<CardViewModel> list = advancedFilterViewModel.CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList();
			if (list.Count > 0)
			{
				filterTaskDefault.DefaultDate = TaskDefaultDao.GetDefaultSafely().GetDefaultDateTime();
				CardViewModel cardViewModel = list.FirstOrDefault((CardViewModel f) => f is PriorityCardViewModel);
				if (cardViewModel != null)
				{
					filterTaskDefault.Priority = (CheckAdvancedFilterValid<PriorityCardViewModel>(advancedFilterViewModel.CardList) ? CalcDefaultPriority((cardViewModel as PriorityCardViewModel)?.Values) : new int?(0));
				}
				filterTaskDefault.ProjectModel = CalculateDefaultProject(advancedFilterViewModel.CardList);
				DateTime? defaultDate = CalculateDefaultDate(advancedFilterViewModel.CardList);
				if (defaultDate.HasValue)
				{
					filterTaskDefault.DefaultDate = defaultDate;
				}
				bool flag = (filterTaskDefault.OnlyNote = advancedFilterViewModel.OnlyNote());
				filterTaskDefault.IsNote = flag || (filterTaskDefault.ProjectModel?.IsNote ?? false);
				filterTaskDefault.HasProjectSelected = advancedFilterViewModel.CardList.Any((CardViewModel card) => card is ProjectOrGroupCardViewModel);
				filterTaskDefault.DefaultTags = CalculateDefaultTag(advancedFilterViewModel.CardList);
			}
		}
		if (!withEmptyProject && filterTaskDefault.ProjectModel == null)
		{
			ProjectModel projectModel = new ProjectModel
			{
				id = TaskDefaultDao.GetDefaultSafely().ProjectId,
				name = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == TaskDefaultDao.GetDefaultSafely().ProjectId)?.name
			};
			filterTaskDefault.ProjectModel = projectModel;
		}
		return filterTaskDefault;
	}

	public static void GetDefaultInNormal(NormalFilterViewModel normal, FilterTaskDefault filterTaskDefault)
	{
		filterTaskDefault.Priority = CalcDefaultPriority(normal.Priorities);
		filterTaskDefault.DefaultTags = CalcDefaultTags(normal.Tags, LogicType.Or);
		DateTime? defaultDate = CalcDefaultDate(normal.DueDates);
		if (defaultDate.HasValue)
		{
			filterTaskDefault.DefaultDate = defaultDate;
		}
		filterTaskDefault.ProjectModel = CalcDefaultProject(normal.Projects, normal.Groups);
		filterTaskDefault.HasProjectSelected = (normal.Projects != null && normal.Projects.Count > 0) || (normal.Groups != null && normal.Groups.Count > 0);
		filterTaskDefault.OnlyNote = normal.OnlyNote();
		filterTaskDefault.IsNote = filterTaskDefault.OnlyNote || (filterTaskDefault.ProjectModel?.IsNote ?? false);
	}

	private static ProjectModel CalculateDefaultProject(IReadOnlyList<CardViewModel> cards)
	{
		if (CheckAdvancedFilterValid<ProjectOrGroupCardViewModel>(cards) && cards != null && cards.Any())
		{
			foreach (CardViewModel card in cards)
			{
				if (card is ProjectOrGroupCardViewModel projectOrGroupCardViewModel)
				{
					ProjectModel projectModel = CalcDefaultProject(projectOrGroupCardViewModel.Values, projectOrGroupCardViewModel.GroupIds, projectOrGroupCardViewModel.LogicType, withDefault: false);
					if (projectModel != null)
					{
						return projectModel;
					}
				}
			}
		}
		return CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == TaskDefaultDao.GetDefaultSafely().ProjectId);
	}

	private static bool CheckAdvancedFilterValid<T>(IReadOnlyList<CardViewModel> cards)
	{
		if (cards.Count((CardViewModel card) => card is T) > 1)
		{
			for (int num = 2; num < cards.Count; num++)
			{
				if (cards[num] is T)
				{
					CardViewModel cardViewModel = cards[num - 1];
					if (cardViewModel != null && cardViewModel.Type == CardType.LogicAnd)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private static DateTime? CalculateDefaultDate(IReadOnlyList<CardViewModel> cards)
	{
		if (CheckAdvancedFilterValid<DateCardViewModel>(cards) && cards != null && cards.Any())
		{
			foreach (CardViewModel card in cards)
			{
				if (card is DateCardViewModel dateCardViewModel)
				{
					DateTime? result = CalcDefaultDate(dateCardViewModel.Values, dateCardViewModel.LogicType);
					if (result.HasValue)
					{
						return result;
					}
				}
			}
		}
		return null;
	}

	private static List<string> CalculateDefaultTag(IReadOnlyList<CardViewModel> cards)
	{
		List<string> list = new List<string>();
		if (IsWithAndWithoutTags(cards))
		{
			return new List<string> { TagDataHelper.GetFirstValidTag() };
		}
		for (int i = 0; i < cards.Count; i++)
		{
			if (!(cards[i] is TagCardViewModel tagCardViewModel))
			{
				continue;
			}
			if (!list.Any())
			{
				list.AddRange(CalcDefaultTags(tagCardViewModel.Values, tagCardViewModel.LogicType));
			}
			else if (i > 0)
			{
				CardViewModel cardViewModel = cards[i - 1];
				if (cardViewModel != null && cardViewModel.Type == CardType.LogicAnd)
				{
					list.AddRange(CalcDefaultTags(tagCardViewModel.Values, tagCardViewModel.LogicType));
				}
			}
		}
		return list.Distinct().ToList();
	}

	private static bool IsWithAndWithoutTags(IReadOnlyList<CardViewModel> cards)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < cards.Count; i++)
		{
			if (i != 0)
			{
				if (i <= 0)
				{
					continue;
				}
				CardViewModel cardViewModel = cards[i - 1];
				if (cardViewModel == null || cardViewModel.Type != CardType.LogicAnd)
				{
					continue;
				}
			}
			if (cards[i] is TagCardViewModel tagCardViewModel)
			{
				list.AddRange(tagCardViewModel.Values);
			}
		}
		if (list.Contains("*withtags"))
		{
			return list.Contains("!tag");
		}
		return false;
	}

	private static ProjectModel CalcDefaultProject(List<string> projectIds, List<string> groupIds, LogicType logicType = LogicType.Or, bool withDefault = true)
	{
		List<ProjectModel> projects = CacheManager.GetProjects();
		ProjectModel projectModel = projects.FirstOrDefault((ProjectModel p) => p.id == TaskDefaultDao.GetDefaultSafely().ProjectId);
		if (!Parser.CheckProjectOrGroupValid(projectIds, groupIds))
		{
			return projectModel;
		}
		if (projectIds.Contains(projectModel?.id) && logicType != LogicType.Not)
		{
			return projectModel;
		}
		projects = projects.Where((ProjectModel project) => !project.Ishide && !project.delete_status && project.IsEnable()).ToList();
		if (logicType == LogicType.Not)
		{
			if (!string.IsNullOrEmpty(projectModel?.id) && !projectIds.Contains(projectModel.id))
			{
				return projectModel;
			}
			return (from p in projects
				where !projectIds.Contains(p.id)
				orderby p.sortOrder
				select p).ToList().FirstOrDefault();
		}
		List<ProjectModel> list = (from p in projects
			where projectIds.Contains(p.id) || groupIds.Contains(p.groupId)
			orderby p.sortOrder
			select p).ToList();
		if (list.Count <= 0)
		{
			if (!withDefault)
			{
				return null;
			}
			return projectModel;
		}
		return list[0];
	}

	private static int? CalcDefaultPriority(IReadOnlyCollection<int> priorities)
	{
		if (priorities == null || priorities.Count == 0)
		{
			return null;
		}
		return priorities.Max();
	}

	private static string GetSelectedTag(List<string> tags)
	{
		string text = tags.Where((string t) => t != "!tag").ToList().FirstOrDefault();
		if (text != null)
		{
			if (!tags.Contains("*withtags"))
			{
				return text;
			}
			return TagDataHelper.GetFirstValidTag();
		}
		return string.Empty;
	}

	private static List<string> CalcDefaultTags(List<string> tags, LogicType logic)
	{
		if (tags == null || tags.Count == 0)
		{
			return TaskDefaultDao.GetDefaultSafely().Tags;
		}
		List<string> list = new List<string>();
		if (logic == LogicType.Not)
		{
			if (tags.Count != 1 || !(tags[0] == "!tag"))
			{
				if (tags.Count > 1 && tags.Contains("!tag"))
				{
					string text = (from item in TagDataHelper.GetTags()
						select item.name).FirstOrDefault((string tag) => !tags.Contains(tag));
					if (text == null)
					{
						return new List<string>();
					}
					return new List<string> { text };
				}
				return new List<string>();
			}
			list.Add(TagDataHelper.GetFirstValidTag());
		}
		if (logic == LogicType.Or)
		{
			if (tags.Count == 1 && tags[0] == "!tag")
			{
				return new List<string>();
			}
			list.Add(GetSelectedTag(tags));
		}
		else
		{
			if (tags.Any() && tags.Contains("!tag"))
			{
				return new List<string>();
			}
			foreach (string tag in tags)
			{
				list.Add((tag == "*withtags") ? TagDataHelper.GetFirstValidTag() : tag);
			}
			list = list.Distinct().ToList();
		}
		if (list.Count <= 0)
		{
			return TaskDefaultDao.GetDefaultSafely().Tags;
		}
		return list.ToList();
	}

	private static DateTime? CalcDefaultDate(ICollection<string> selectedDates, LogicType logic = LogicType.Or)
	{
		if (logic == LogicType.Not)
		{
			selectedDates.Remove("nodue");
			selectedDates.Remove("recurring");
			if (selectedDates.Count == 0)
			{
				return DateTime.Today;
			}
			return null;
		}
		List<DefaultDateModel> list = (from date in DefaultDateModel.GetDefaultDateMap()
			where selectedDates.Contains(date.Key)
			select date).ToList();
		if (list.Any((DefaultDateModel d) => d.DefaultDate.HasValue && d.DefaultDate.Value == DateTime.Today))
		{
			return DateTime.Today;
		}
		if (selectedDates.Contains("nextweek"))
		{
			int nextWeekDayDiff = DateUtils.GetNextWeekDayDiff();
			list.Add(new DefaultDateModel
			{
				Key = "nextweek",
				Priority = nextWeekDayDiff,
				DefaultDate = DateTime.Today.AddDays(nextWeekDayDiff)
			});
		}
		if (selectedDates.FirstOrDefault((string date) => date.EndsWith("days")) != null)
		{
			return DateTime.Today;
		}
		foreach (string item in from offset in selectedDates.Where((string date) => date.StartsWith("offset")).ToList()
			where offset.Length > 8
			select offset.Substring(7) into val
			select val.Substring(0, val.Length - 1))
		{
			if (!int.TryParse(item.Substring(0, item.Length - 1), out var result))
			{
				continue;
			}
			string text = item.Substring(item.Length - 1);
			if (!(text == "W"))
			{
				if (text == "M")
				{
					result = (int)(DateTime.Today.AddDays(1 - DateTime.Today.Day).AddMonths(result) - DateTime.Today).TotalDays;
				}
			}
			else
			{
				result = (Utils.GetWeekStart(DateTime.Today).AddDays(result * 7) - DateTime.Today).Days;
			}
			list.Add(new DefaultDateModel
			{
				Key = $"{7}dayslater",
				Priority = result,
				DefaultDate = DateTime.Now.Date.AddDays(result)
			});
		}
		string text2 = selectedDates.FirstOrDefault((string date) => date.EndsWith("dayslater"));
		if (text2 != null)
		{
			int.TryParse(text2.Replace("dayslater", string.Empty), out var result2);
			list.Add(new DefaultDateModel
			{
				Key = $"{7}dayslater",
				Priority = result2,
				DefaultDate = DateTime.Now.Date.AddDays(result2)
			});
		}
		List<string> list2 = selectedDates.Where((string date) => date.EndsWith("daysfromtoday")).ToList();
		if (list2.Count > 0)
		{
			foreach (string item2 in list2)
			{
				if (int.TryParse(item2.Replace("daysfromtoday", string.Empty), out var result3))
				{
					if (result3 == 0)
					{
						return DateTime.Today;
					}
					list.Add(new DefaultDateModel
					{
						Key = 7 + "daysfromtoday",
						Priority = result3,
						DefaultDate = DateTime.Now.Date.AddDays(result3)
					});
				}
			}
		}
		string text3 = selectedDates.FirstOrDefault((string d) => d.StartsWith("span"));
		if (text3 != null)
		{
			var (num, num2) = FilterUtils.GetSpanPairInRule(text3);
			if (!num.HasValue && !num2.HasValue)
			{
				return DateTime.Today;
			}
			int num3 = 0;
			num3 = ((!num.HasValue) ? ((num2.Value < 0) ? num2.Value : 0) : (num2.HasValue ? ((num2.Value < 0) ? num2.Value : ((num.Value > 0) ? num.Value : 0)) : ((num.Value > 0) ? num.Value : 0)));
			if (num3 == 0)
			{
				return DateTime.Today;
			}
			list.Add(new DefaultDateModel
			{
				Key = text3,
				Priority = num3,
				DefaultDate = DateTime.Now.Date.AddDays(num3)
			});
		}
		list = list.OrderBy((DefaultDateModel date) => date.Priority).ToList();
		DefaultDateModel defaultDateModel = list.FirstOrDefault((DefaultDateModel d) => d.Priority >= 0);
		if (defaultDateModel != null && defaultDateModel.DefaultDate.HasValue)
		{
			return defaultDateModel.DefaultDate;
		}
		if (defaultDateModel == null || defaultDateModel.Key == "nodue")
		{
			DefaultDateModel defaultDateModel2 = list.LastOrDefault((DefaultDateModel d) => d.Priority < 0);
			if (defaultDateModel2 != null)
			{
				return defaultDateModel2.DefaultDate;
			}
		}
		if (defaultDateModel != null)
		{
			return defaultDateModel.DefaultDate;
		}
		return TaskDefaultDao.GetDefaultSafely().GetDefaultDateTime();
	}
}
