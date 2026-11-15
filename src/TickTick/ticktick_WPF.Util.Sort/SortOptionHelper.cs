using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Views.TaskList;

namespace ticktick_WPF.Util.Sort;

public static class SortOptionHelper
{
	private static Dictionary<string, SortTypeViewModel> GetSortTypeDictionary(bool isTimeline = false)
	{
		return new Dictionary<string, SortTypeViewModel>
		{
			{
				Constants.SortType.sortOrder.ToString(),
				new SortTypeViewModel(Constants.SortType.sortOrder.ToString(), isTimeline)
			},
			{
				Constants.SortType.project.ToString(),
				new SortTypeViewModel(Constants.SortType.project.ToString())
			},
			{
				Constants.SortType.dueDate.ToString(),
				new SortTypeViewModel(Constants.SortType.dueDate.ToString())
			},
			{
				Constants.SortType.title.ToString(),
				new SortTypeViewModel(Constants.SortType.title.ToString())
			},
			{
				Constants.SortType.priority.ToString(),
				new SortTypeViewModel(Constants.SortType.priority.ToString())
			},
			{
				Constants.SortType.tag.ToString(),
				new SortTypeViewModel(Constants.SortType.tag.ToString())
			},
			{
				Constants.SortType.assignee.ToString(),
				new SortTypeViewModel(Constants.SortType.assignee.ToString())
			},
			{
				Constants.SortType.createdTime.ToString(),
				new SortTypeViewModel(Constants.SortType.createdTime.ToString())
			},
			{
				Constants.SortType.modifiedTime.ToString(),
				new SortTypeViewModel(Constants.SortType.modifiedTime.ToString())
			}
		};
	}

	public static Dictionary<string, string> GetSortOptionDict(bool normalProject, bool withShare, bool isTag = false, bool isTagParent = false, TaskType kind = TaskType.Task)
	{
		List<Constants.SortType> list = null;
		return ((IEnumerable<Constants.SortType>)(kind switch
		{
			TaskType.Task => (!isTag) ? ((!normalProject) ? new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.priority
			} : ((!withShare) ? new List<Constants.SortType>
			{
				Constants.SortType.sortOrder,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.priority
			} : new List<Constants.SortType>
			{
				Constants.SortType.sortOrder,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.priority,
				Constants.SortType.assignee
			})) : ((!isTagParent) ? new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.priority
			} : new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.priority
			}), 
			TaskType.Note => (!normalProject) ? new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag
			} : ((!withShare) ? new List<Constants.SortType>
			{
				Constants.SortType.sortOrder,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag
			} : new List<Constants.SortType>
			{
				Constants.SortType.sortOrder,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.assignee
			}), 
			_ => new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.priority
			}, 
		}))?.ToDictionary((Constants.SortType x) => x.ToString(), (Constants.SortType x) => SortTypeViewModel.GetTitleByType(x));
	}

	public static Dictionary<string, string> GetSortOptionDictInGroup(TaskType kind = TaskType.Task)
	{
		List<Constants.SortType> list = new List<Constants.SortType>
		{
			Constants.SortType.project,
			Constants.SortType.dueDate,
			Constants.SortType.title,
			Constants.SortType.tag,
			Constants.SortType.priority
		};
		if (kind == TaskType.Task)
		{
			list.Insert(2, Constants.SortType.createdTime);
			list.Insert(3, Constants.SortType.modifiedTime);
		}
		else
		{
			list.Insert((kind == TaskType.Note) ? 1 : 5, Constants.SortType.createdTime);
			list.Insert((kind == TaskType.Note) ? 2 : 6, Constants.SortType.modifiedTime);
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (Constants.SortType item in list)
		{
			if ((item != Constants.SortType.dueDate && item != Constants.SortType.priority) || kind != TaskType.Note)
			{
				dictionary.Add(item.ToString(), SortTypeViewModel.GetTitleByType(item));
			}
		}
		return dictionary;
	}

	private static List<SortTypeViewModel> BuildSortTypeModels(IEnumerable<Constants.SortType> keys, bool isTimeline = false)
	{
		Dictionary<string, SortTypeViewModel> dict = GetSortTypeDictionary(isTimeline);
		return keys.Select((Constants.SortType k) => dict[k.ToString()]).ToList();
	}

	public static List<SortTypeViewModel> GetNormalProjectSortTypeModels(bool isShare = false, bool isNote = false, bool isTimeline = false)
	{
		List<Constants.SortType> list = (isTimeline ? new List<Constants.SortType>
		{
			Constants.SortType.sortOrder,
			Constants.SortType.dueDate,
			Constants.SortType.tag,
			Constants.SortType.priority
		} : ((!isNote) ? new List<Constants.SortType>
		{
			Constants.SortType.sortOrder,
			Constants.SortType.dueDate,
			Constants.SortType.createdTime,
			Constants.SortType.modifiedTime,
			Constants.SortType.title,
			Constants.SortType.tag,
			Constants.SortType.priority
		} : new List<Constants.SortType>
		{
			Constants.SortType.sortOrder,
			Constants.SortType.createdTime,
			Constants.SortType.modifiedTime,
			Constants.SortType.title,
			Constants.SortType.tag
		}));
		if (isShare)
		{
			list.Add(Constants.SortType.assignee);
		}
		return BuildSortTypeModels(list, isTimeline);
	}

	public static List<SortTypeViewModel> GetGroupTimelineSortTypeModels(bool isShare = false)
	{
		List<Constants.SortType> list = new List<Constants.SortType>
		{
			Constants.SortType.project,
			Constants.SortType.dueDate,
			Constants.SortType.tag,
			Constants.SortType.priority
		};
		if (isShare)
		{
			list.Add(Constants.SortType.assignee);
		}
		return BuildSortTypeModels(list);
	}

	public static List<SortTypeViewModel> GetSmartProjectSortTypeModels(bool isNote = false, bool inTimeline = false)
	{
		List<Constants.SortType> keys = (inTimeline ? new List<Constants.SortType>
		{
			Constants.SortType.project,
			Constants.SortType.dueDate,
			Constants.SortType.tag,
			Constants.SortType.priority
		} : ((!isNote) ? new List<Constants.SortType>
		{
			Constants.SortType.project,
			Constants.SortType.dueDate,
			Constants.SortType.createdTime,
			Constants.SortType.modifiedTime,
			Constants.SortType.title,
			Constants.SortType.tag,
			Constants.SortType.priority
		} : new List<Constants.SortType>
		{
			Constants.SortType.createdTime,
			Constants.SortType.modifiedTime,
			Constants.SortType.project,
			Constants.SortType.title,
			Constants.SortType.tag
		}));
		return BuildSortTypeModels(keys);
	}

	public static List<SortTypeViewModel> GetTagProjectSortTypeModels(bool showByTag)
	{
		List<Constants.SortType> list = new List<Constants.SortType>
		{
			Constants.SortType.project,
			Constants.SortType.dueDate,
			Constants.SortType.createdTime,
			Constants.SortType.modifiedTime,
			Constants.SortType.title,
			Constants.SortType.priority
		};
		if (showByTag)
		{
			list.Insert(list.Count - 1, Constants.SortType.tag);
		}
		return BuildSortTypeModels(list);
	}

	public static List<SortTypeViewModel> GetGroupProjectSortTypeModels(int displayKind, bool isShare = false)
	{
		List<Constants.SortType> list = displayKind switch
		{
			1 => new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag
			}, 
			2 => new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.priority
			}, 
			_ => new List<Constants.SortType>
			{
				Constants.SortType.project,
				Constants.SortType.dueDate,
				Constants.SortType.createdTime,
				Constants.SortType.modifiedTime,
				Constants.SortType.title,
				Constants.SortType.tag,
				Constants.SortType.priority
			}, 
		};
		if (isShare)
		{
			list.Add(Constants.SortType.assignee);
		}
		return BuildSortTypeModels(list);
	}

	public static DrawingImage GetSortTypeImage(Constants.SortType sortType, bool isTimeline = false)
	{
		return sortType switch
		{
			Constants.SortType.sortOrder => Utils.GetImageSource(isTimeline ? "TimelineDefaultSortOrderDrawingImage" : "customDrawingImage"), 
			Constants.SortType.project => Utils.GetImageSource("SortByListDrawingImage"), 
			Constants.SortType.dueDate => Utils.GetImageSource("SortByDateDrawingImage"), 
			Constants.SortType.title => Utils.GetImageSource("SortByTitleDrawingImage"), 
			Constants.SortType.priority => Utils.GetImageSource("SortByPriorityDrawingImage"), 
			Constants.SortType.assignee => Utils.GetImageSource("SortByAssigneeDrawingImage"), 
			Constants.SortType.tag => Utils.GetImageSource("SortByTagDrawingImage"), 
			Constants.SortType.createdTime => Utils.GetImageSource("SortByCreateTimeDrawingImage"), 
			Constants.SortType.modifiedTime => Utils.GetImageSource("SortByModifiedTimeDrawingImage"), 
			_ => null, 
		};
	}
}
