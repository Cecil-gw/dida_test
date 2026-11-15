using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Search;

public static class SearchProjectHelper
{
	private static readonly BlockingList<SearchTagAndProjectModel> ProjectModels = new BlockingList<SearchTagAndProjectModel>();

	public static void InitModels()
	{
		ProjectModels.Clear();
		CacheManager.GetTags().ForEach(delegate(TagModel tag)
		{
			ProjectModels.Add(new SearchTagAndProjectModel(tag));
		});
		CacheManager.GetProjectGroups().ForEach(delegate(ProjectGroupModel group)
		{
			ProjectModels.Add(new SearchTagAndProjectModel(group));
		});
		IEnumerable<ProjectModel> enumerable = CacheManager.GetProjects()?.Where((ProjectModel p) => p.IsValid());
		if (enumerable != null)
		{
			foreach (ProjectModel item in enumerable)
			{
				ProjectModels.Add(new SearchTagAndProjectModel(item));
			}
		}
		IEnumerable<FilterModel> enumerable2 = CacheManager.GetFilters()?.Where((FilterModel f) => f.IsAvailable());
		if (enumerable2 == null)
		{
			return;
		}
		foreach (FilterModel item2 in enumerable2)
		{
			ProjectModels.Add(new SearchTagAndProjectModel(item2));
		}
	}

	public static void UpdateModels(ProjectGroupModel group)
	{
		SearchTagAndProjectModel searchTagAndProjectModel = ProjectModels.FirstOrDefault((SearchTagAndProjectModel model) => model.IsGroup && model.Id == group.id);
		if (searchTagAndProjectModel != null)
		{
			ProjectModels.Remove(searchTagAndProjectModel);
		}
		ProjectModels.Add(new SearchTagAndProjectModel(group));
	}

	public static void DeleteGroup(string id)
	{
		SearchTagAndProjectModel searchTagAndProjectModel = ProjectModels.FirstOrDefault((SearchTagAndProjectModel model) => model.IsGroup && model.Id == id);
		if (searchTagAndProjectModel != null)
		{
			ProjectModels.Remove(searchTagAndProjectModel);
		}
	}

	public static void UpdateModels(FilterModel filter)
	{
		SearchTagAndProjectModel searchTagAndProjectModel = ProjectModels.FirstOrDefault((SearchTagAndProjectModel model) => model.IsFilter && model.Id == filter.id);
		if (searchTagAndProjectModel != null)
		{
			ProjectModels.Remove(searchTagAndProjectModel);
		}
		if (filter.IsAvailable())
		{
			ProjectModels.Add(new SearchTagAndProjectModel(filter));
		}
	}

	public static void DeleteFilter(string id)
	{
		SearchTagAndProjectModel searchTagAndProjectModel = ProjectModels.FirstOrDefault((SearchTagAndProjectModel model) => model.IsFilter && model.Id == id);
		if (searchTagAndProjectModel != null)
		{
			ProjectModels.Remove(searchTagAndProjectModel);
		}
	}

	public static void UpdateModels(ProjectModel project)
	{
		SearchTagAndProjectModel searchTagAndProjectModel = ProjectModels.FirstOrDefault((SearchTagAndProjectModel model) => model.Id == project.id && !model.IsTag);
		if (searchTagAndProjectModel != null)
		{
			ProjectModels.Remove(searchTagAndProjectModel);
		}
		if (project.IsEnable())
		{
			ProjectModels.Add(new SearchTagAndProjectModel(project));
		}
	}

	public static void DeleteModels(ProjectModel project)
	{
		SearchTagAndProjectModel searchTagAndProjectModel = ProjectModels.FirstOrDefault((SearchTagAndProjectModel m) => m.Id == project.id && !m.IsTag);
		if (searchTagAndProjectModel != null)
		{
			ProjectModels.Remove(searchTagAndProjectModel);
		}
	}

	public static void DeleteModels(string tag)
	{
		SearchTagAndProjectModel searchTagAndProjectModel = ProjectModels.FirstOrDefault((SearchTagAndProjectModel m) => m.Id == tag && m.IsTag);
		if (searchTagAndProjectModel != null)
		{
			ProjectModels.Remove(searchTagAndProjectModel);
		}
	}

	public static void UpdateModels(TagModel tag)
	{
		SearchTagAndProjectModel m = ProjectModels.FirstOrDefault((SearchTagAndProjectModel model) => model.Id == tag.name && model.IsTag);
		ProjectModels.Remove(m);
		ProjectModels.Add(new SearchTagAndProjectModel(tag));
	}

	private static List<SearchTagAndProjectModel> GetModels(bool onlyProject, bool withInbox = false)
	{
		List<SearchTagAndProjectModel> list = new List<SearchTagAndProjectModel>();
		foreach (SearchTagAndProjectModel item in ProjectModels.Value)
		{
			if (item.IsProject)
			{
				if (withInbox || !item.IsInbox)
				{
					list.Add(item);
				}
			}
			else
			{
				if (onlyProject)
				{
					continue;
				}
				if (item.IsTag)
				{
					switch (LocalSettings.Settings.SmartListTag)
					{
					case 2:
					{
						ConcurrentDictionary<string, int> countData = TaskCountCache.CountData;
						string key = item.Id.ToLower();
						if (countData.TryGetValue(key, out var value) && value > 0)
						{
							list.Add(item);
						}
						break;
					}
					case 0:
						list.Add(item);
						break;
					}
				}
				else
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	public static void Clear()
	{
		ProjectModels?.Clear();
		SearchHistoryDao.Clear();
	}

	public static List<SearchTagAndProjectModel> GetModelsMatched(string text, bool onlyProject = false, bool withInbox = false)
	{
		List<SearchTagAndProjectModel> source = GetModels(onlyProject, withInbox);
		if (!string.IsNullOrWhiteSpace(text))
		{
			List<string> keys = text.Split(' ').ToList();
			keys.RemoveAll((string key) => key == "");
			source = source.Where((SearchTagAndProjectModel m) => m.Pinyin.Contains(text) || SearchHelper.KeyWordMatch(m.Emoji + m.Name.ToLower(), text.Trim(), keys) != 0).ToList();
		}
		return source.OrderBy((SearchTagAndProjectModel it) => it.SortOrder).ToList();
	}

	public static async Task<List<SearchTagAndProjectModel>> GetTaskGroupsMatched(string text, bool withInbox = false)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return new List<SearchTagAndProjectModel>();
		}
		Dictionary<string, ProjectModel> projects = (from project in CacheManager.GetProjects()?.Where((ProjectModel project) => !string.IsNullOrEmpty(project.id) && project.IsValid() && (withInbox || !project.Isinbox))
			group project by project.id).ToDictionary((IGrouping<string, ProjectModel> group) => group.Key, (IGrouping<string, ProjectModel> group) => group.First()) ?? new Dictionary<string, ProjectModel>();
		List<ColumnModel> obj = await ColumnDao.GetAllColumnsAsync();
		List<SearchTagAndProjectModel> list = new List<SearchTagAndProjectModel>();
		foreach (ColumnModel item in obj)
		{
			if (!string.IsNullOrWhiteSpace(item.name) && !string.IsNullOrWhiteSpace(item.projectId) && projects.TryGetValue(item.projectId, out var value))
			{
				list.Add(new SearchTagAndProjectModel(item, value));
			}
		}
		string normalizedText = text.Trim().ToLower();
		List<string> keys = (from key in normalizedText.Split(' ')
			where key != string.Empty
			select key).ToList();
		return (from model in list
			where model.Pinyin.Contains(normalizedText) || SearchHelper.KeyWordMatch(model.Name.ToLower(), normalizedText, keys) != 0
			orderby model.ParentSortOrder, model.SortOrder
			select model).ToList();
	}

	private static string Replace(string name, string old, string newStr)
	{
		int num = name.IndexOf(old, StringComparison.Ordinal);
		if (num >= 0)
		{
			name = name.Remove(num, old.Length);
			return name.Insert(num, newStr);
		}
		return name;
	}

	public static void UpdateSortOrder()
	{
		ProjectModels.Foreach(delegate(SearchTagAndProjectModel m)
		{
			m.UpdateGroupSortOrder();
		});
		lock (ProjectModels.Value)
		{
			ProjectModels.Value.Sort(delegate(SearchTagAndProjectModel a, SearchTagAndProjectModel b)
			{
				if (a.IsTag && !b.IsTag)
				{
					return -1;
				}
				if (b.IsTag && !a.IsTag)
				{
					return 1;
				}
				if (a.IsProject && b.IsFilter)
				{
					return -1;
				}
				if (b.IsProject && a.IsFilter)
				{
					return 1;
				}
				if ((a.IsTag && b.IsTag) || (a.IsFilter && b.IsFilter))
				{
					return a.SortOrder.CompareTo(b.SortOrder);
				}
				if (a.IsProject && b.IsProject)
				{
					if (a.TeamProject != b.TeamProject)
					{
						return a.TeamProject.CompareTo(b.TeamProject);
					}
					if (a.ParentSortOrder.HasValue && !b.ParentSortOrder.HasValue)
					{
						return a.ParentSortOrder.Value.CompareTo(b.SortOrder);
					}
					if (!a.ParentSortOrder.HasValue && b.ParentSortOrder.HasValue)
					{
						return a.SortOrder.CompareTo(b.ParentSortOrder.Value);
					}
					if (a.ParentSortOrder.HasValue && b.ParentSortOrder.HasValue && a.ParentSortOrder != b.ParentSortOrder)
					{
						return a.ParentSortOrder.Value.CompareTo(b.ParentSortOrder.Value);
					}
					return a.SortOrder.CompareTo(b.SortOrder);
				}
				return 0;
			});
		}
	}
}
