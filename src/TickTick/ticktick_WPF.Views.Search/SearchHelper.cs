using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

public static class SearchHelper
{
	private static string _searchKey;

	public static SearchFilterViewModel SearchFilter = new SearchFilterViewModel();

	private static SolidColorBrush _searchHighlightColor;

	private static SolidColorBrush _darkSearchHighlightColor;

	public static List<string> Tags;

	private static SearchKind _searchKind = SearchKind.Normal;

	private static SearchCategory _searchCategory = SearchCategory.None;

	public static Regex PreSearchRegex = new Regex("");

	public static Regex SearchRegex = new Regex("");

	private static BlockingList<TaskSearchModel> _tempSearchModel;

	private static string _tempSearchModelKey;

	private static BlockingList<TaskSearchModel> _tempRecentCreatedSearchModel;

	public static SearchKind SearchKind
	{
		get
		{
			return _searchKind;
		}
		set
		{
			_searchKind = value;
		}
	}

	public static SearchCategory SearchCategory
	{
		get
		{
			return _searchCategory;
		}
		set
		{
			_searchCategory = value;
		}
	}

	public static string SearchKey
	{
		get
		{
			return _searchKey;
		}
		set
		{
			_searchKey = value;
			List<string> list = value?.ToLower().Split(' ').ToList();
			list?.Remove("");
			string text = "(" + Utils.HandleReg(SearchKey?.ToLower()) + ")";
			if (list != null && list.Count > 1)
			{
				foreach (string item in list)
				{
					text = text + "|(" + Utils.HandleReg(item) + ")";
				}
			}
			SearchRegex = new Regex(text);
			SearchHelper.SearchKeyChanged?.Invoke(null, null);
		}
	}

	public static bool DetailChanged { get; set; }

	public static bool InSearch
	{
		get
		{
			return LocalSettings.Settings.InSearch;
		}
		set
		{
			if (LocalSettings.Settings.InSearch != value)
			{
				LocalSettings.Settings.InSearch = value;
				SearchHelper.SearchModeChanged?.Invoke(null, EventArgs.Empty);
			}
		}
	}

	public static event EventHandler SearchKeyChanged;

	public static event EventHandler SearchModeChanged;

	public static int KeyWordMatch(string text, string keyword, List<string> keys, bool ignoreCase = true)
	{
		if (ignoreCase)
		{
			text = text.ToLower();
			keyword = keyword.ToLower();
			keys = keys.Select((string it) => it.ToLower()).ToList();
		}
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		if (text.Contains(keyword))
		{
			return 1;
		}
		if (keys.Count > 0)
		{
			bool flag = true;
			foreach (string key in keys)
			{
				if (text.Contains(key))
				{
					Utils.ReplaceFirst(ref text, key, "");
					continue;
				}
				flag = false;
				break;
			}
			if (flag)
			{
				return 2;
			}
		}
		return 0;
	}

	public static bool KeyWordMatched(string text, string keyword, List<string> keys)
	{
		return KeyWordMatch(text, keyword, keys) > 0;
	}

	public static async void TrySearchFromService(string searchKey, SearchFilterModel filterModel)
	{
		if (filterModel == null || filterModel.Category != SearchCategory.CalendarEvent)
		{
			SearchResultModel searchedResult = await Communicator.GetSearchedResult(searchKey, filterModel);
			bool flag = searchedResult?.Tasks != null;
			if (flag)
			{
				flag = await TaskService.MergeTasks(searchedResult.Tasks);
			}
			bool taskMerged = flag;
			flag = searchedResult?.Comments != null;
			if (flag)
			{
				flag = await CommentService.MergeComments(searchedResult.Comments);
			}
			bool flag2 = flag;
			if (taskMerged || flag2)
			{
				ClearTaskSearchModels();
				App.Window.ListView?.ReSearch();
			}
		}
	}

	public static Brush GetSearchHighlightColor()
	{
		if (LocalSettings.Settings.IsDarkTheme)
		{
			if (_darkSearchHighlightColor == null)
			{
				_darkSearchHighlightColor = ThemeUtil.GetColorInString("#CCFFD54D");
			}
			return _darkSearchHighlightColor ?? Brushes.Transparent;
		}
		if (_searchHighlightColor == null)
		{
			_searchHighlightColor = ThemeUtil.GetColorInString("#FFD54D");
		}
		return _searchHighlightColor ?? Brushes.Transparent;
	}

	public static async Task<List<TaskSearchModel>> GetTaskSearchModels(SearchFilterModel filter, List<string> tags)
	{
		string key = GetTaskSearchModelKey(filter, tags);
		if (_tempSearchModel == null || _tempSearchModelKey != key)
		{
			_tempSearchModel = new BlockingList<TaskSearchModel>(await TaskViewModelHelper.GetTaskSearchModels(filter, tags));
			_tempSearchModelKey = key;
		}
		return _tempSearchModel.Value;
	}

	private static string GetTaskSearchModelKey(SearchFilterModel filter, List<string> tags)
	{
		if (filter == null)
		{
			return string.Empty;
		}
		return string.Join("|", filter.Category, filter.Kind, filter.TaskType, filter.Status?.ToString() ?? string.Empty, filter.DateFilter, filter.Start?.Ticks.ToString() ?? string.Empty, filter.End?.Ticks.ToString() ?? string.Empty, Join(filter.ProjectIds), Join(filter.GroupIds), Join(filter.CalendarAccountIds), Join(filter.CalendarIds), Join(filter.Tags), JoinInts(filter.Priorities), Join(filter.Assignees), Join(filter.ExcludeTaskIds), Join(tags));
		static string Join(IEnumerable<string> values)
		{
			if (values != null)
			{
				return string.Join(",", values.OrderBy((string v) => v));
			}
			return string.Empty;
		}
		static string JoinInts(IEnumerable<int> values)
		{
			if (values != null)
			{
				return string.Join(",", values.OrderBy((int v) => v));
			}
			return string.Empty;
		}
	}

	public static async Task<List<TaskSearchModel>> GetRecentCreatedSearchModels(int limit, List<string> exceptTaskIds)
	{
		_tempRecentCreatedSearchModel = new BlockingList<TaskSearchModel>(await TaskViewModelHelper.GetRecentCreatedTasks(limit, exceptTaskIds));
		return _tempRecentCreatedSearchModel.Value;
	}

	public static async Task OnEventArchiveChanged(string eventId)
	{
		TaskSearchModel model = _tempSearchModel?.FirstOrDefault((TaskSearchModel m) => m.Id == eventId);
		if (model == null)
		{
			return;
		}
		if (!string.IsNullOrEmpty(model.SourceModel?.RepeatFlag))
		{
			CalendarEventModel eve = await CalendarEventDao.GetEventById(eventId);
			DateTime? eventNextRepeat = CalendarEventDao.GetEventNextRepeat(eve, await CalendarEventDao.GetSkipEvents(), await ArchivedDao.GetArchivedKeys());
			if (eventNextRepeat.HasValue)
			{
				double? num = (eve.DueEnd - eve.DueStart)?.TotalMinutes;
				eve.DueStart = eventNextRepeat;
				if (num.HasValue)
				{
					eve.DueEnd = eventNextRepeat.Value.AddMinutes(num.Value);
				}
				model.SourceModel = new TaskBaseViewModel(eve);
				model.Content = eve.Content;
			}
			else
			{
				_tempSearchModel?.Remove(model);
			}
		}
		else
		{
			_tempSearchModel?.Remove(model);
		}
	}

	public static void ClearTaskSearchModels()
	{
		_tempSearchModel = null;
		_tempSearchModelKey = null;
	}
}
