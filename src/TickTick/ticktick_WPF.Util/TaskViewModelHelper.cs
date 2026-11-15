using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Project;
using ticktick_WPF.Views.Search;
using ticktick_WPF.Views.Summary;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util;

public static class TaskViewModelHelper
{
	public static bool Loaded { get; set; }

	public static async Task Init()
	{
		_ = 1;
		try
		{
			await TaskDetailItemCache.InitLocalCheckItems(await TaskCache.InitLocalTasks());
		}
		finally
		{
			Loaded = true;
		}
	}

	public static List<TaskBaseViewModel> GetModel(List<string> groups = null, List<string> projects = null, List<string> tags = null, List<FilterDatePair> startEndPairs = null, bool showComplete = true, bool showCheckItem = false, bool inTodayOrWeek = false, bool inAll = false, List<string> assignTo = null, List<int> priorities = null, LogicType type = LogicType.Or, bool withChild = false, bool inCal = false, bool orTag = false, TaskType taskType = TaskType.TaskAndNote, string keyword = null)
	{
		List<TaskBaseViewModel> list = (from t in GetTasks(TaskCache.GetAllTask(), groups, projects, tags, startEndPairs, showComplete, inTodayOrWeek, inAll, assignTo, priorities, type, withChild, inCal, orTag, taskType, keyword)
			where t != null
			select t).ToList();
		if (showCheckItem && taskType != TaskType.Note)
		{
			IEnumerable<TaskBaseViewModel> items = GetItems(TaskDetailItemCache.GetAllCheckItems(), groups, projects, tags, startEndPairs, showComplete, inTodayOrWeek, inAll, assignTo, priorities, type, orTag, keyword);
			list.AddRange(items);
		}
		return list;
	}

	public static IEnumerable<TaskBaseViewModel> GetItems(List<TaskBaseViewModel> allItems, List<string> groups = null, List<string> projects = null, List<string> tags = null, List<FilterDatePair> startEndPairs = null, bool showComplete = true, bool inTodayOrWeek = false, bool checkInAll = false, List<string> assignTo = null, List<int> priorities = null, LogicType type = LogicType.Or, bool orTag = false, string keyword = null)
	{
		Dictionary<string, ProjectModel> projectDicts = CacheManager.GetProjects().ToDictionary((ProjectModel p) => p.id, (ProjectModel p) => p);
		keyword = keyword?.Trim().ToLower();
		List<string> keys = keyword?.Split(' ').ToList();
		keys?.Remove("");
		return allItems.Where(delegate(TaskBaseViewModel item)
		{
			if (string.IsNullOrEmpty(item.ParentId) || !TaskCache.LocalTaskViewModels.ContainsKey(item.ParentId))
			{
				return false;
			}
			TaskBaseViewModel taskBaseViewModel = TaskCache.LocalTaskViewModels[item.ParentId];
			if (Utils.IsEmptyDate(item.StartDate))
			{
				return false;
			}
			if (!CheckTask(taskBaseViewModel, showComplete: false) || taskBaseViewModel.Kind != "CHECKLIST")
			{
				return false;
			}
			if (!showComplete && item.Status != 0)
			{
				return false;
			}
			if (string.IsNullOrEmpty(taskBaseViewModel.ProjectId) || !projectDicts.ContainsKey(taskBaseViewModel.ProjectId))
			{
				return false;
			}
			ProjectModel project = projectDicts[taskBaseViewModel.ProjectId];
			if (orTag)
			{
				List<string> list = projects;
				if (list == null || list.Count <= 0)
				{
					List<string> list2 = groups;
					if (list2 == null || list2.Count <= 0)
					{
						goto IL_0129;
					}
				}
				List<string> list3 = tags;
				if (list3 != null && list3.Count > 0)
				{
					if (!CheckProjectAndGroups(project, taskBaseViewModel, projects, groups, checkInAll, type) && !CheckTag(taskBaseViewModel, tags, type))
					{
						return false;
					}
					goto IL_0161;
				}
			}
			goto IL_0129;
			IL_0161:
			if (!CheckPriority(taskBaseViewModel, priorities, type) || !CheckAssignTo(taskBaseViewModel, project, assignTo, type))
			{
				return false;
			}
			bool flag = true;
			List<FilterDatePair> list4 = startEndPairs;
			if (list4 != null && list4.Count > 0)
			{
				bool flag2 = false;
				foreach (FilterDatePair startEndPair in startEndPairs)
				{
					if (!startEndPair.isRepeat)
					{
						DateTime? start = startEndPair.Start;
						DateTime? end = startEndPair.End;
						if (start.HasValue || end.HasValue || startEndPair.IsNoDate != !Utils.IsEmptyDate(item.StartDate))
						{
							if (start.HasValue && !end.HasValue)
							{
								DateTime? startDate = item.StartDate;
								DateTime? dateTime = start;
								if ((startDate.HasValue & dateTime.HasValue) && startDate.GetValueOrDefault() < dateTime.GetValueOrDefault() && !inTodayOrWeek)
								{
									continue;
								}
							}
							if (start.HasValue || !end.HasValue || !(item.StartDate >= end))
							{
								if (!Utils.IsEmptyDate(start) && !Utils.IsEmptyDate(end))
								{
									if (item.StartDate >= end || (!inTodayOrWeek && item.StartDate < start))
									{
										continue;
									}
									if (inTodayOrWeek)
									{
										DateTime? startDate = item.StartDate;
										DateTime? dateTime = start;
										if ((startDate.HasValue & dateTime.HasValue) && startDate.GetValueOrDefault() < dateTime.GetValueOrDefault() && item.Status != 0 && item.CompletedTime < DateTime.Today)
										{
											continue;
										}
									}
								}
								flag2 = true;
								break;
							}
						}
					}
				}
				flag = ((type == LogicType.Or) ? flag2 : (!flag2));
			}
			if (string.IsNullOrEmpty(keyword))
			{
				return flag;
			}
			bool flag3 = SearchHelper.KeyWordMatched(item.Title, keyword, keys);
			if (flag)
			{
				if (type != LogicType.Not)
				{
					return flag3;
				}
				return !flag3;
			}
			return false;
			IL_0129:
			if (!CheckProjectAndGroups(project, taskBaseViewModel, projects, groups, checkInAll, type) || !CheckTag(taskBaseViewModel, tags, type))
			{
				return false;
			}
			goto IL_0161;
		});
	}

	public static IEnumerable<TaskBaseViewModel> GetTasks(List<TaskBaseViewModel> allTasks, List<string> groups = null, List<string> projects = null, List<string> tags = null, List<FilterDatePair> startEndPairs = null, bool showComplete = true, bool inTodayOrWeek = false, bool inAll = false, List<string> assignTo = null, List<int> priorities = null, LogicType type = LogicType.Or, bool withChild = false, bool inCal = false, bool orTag = false, TaskType taskType = TaskType.TaskAndNote, string keyword = null, bool checkProjectPermission = false)
	{
		Dictionary<string, ProjectModel> projectDicts = CacheManager.GetProjects().ToDictionary((ProjectModel p) => p.id, (ProjectModel p) => p);
		List<TaskBaseViewModel> list = allTasks.Where(delegate(TaskBaseViewModel task)
		{
			if ((taskType == TaskType.Note && task.Kind != "NOTE") || (taskType == TaskType.Task && task.Kind == "NOTE"))
			{
				return false;
			}
			if (!CheckTask(task, showComplete))
			{
				return false;
			}
			if (string.IsNullOrEmpty(task.ProjectId) || !projectDicts.ContainsKey(task.ProjectId))
			{
				return false;
			}
			ProjectModel projectModel = projectDicts[task.ProjectId];
			if (orTag)
			{
				List<string> list3 = projects;
				if (list3 == null || list3.Count <= 0)
				{
					List<string> list4 = groups;
					if (list4 == null || list4.Count <= 0)
					{
						goto IL_0100;
					}
				}
				List<string> list5 = tags;
				if (list5 != null && list5.Count > 0)
				{
					if (!CheckProjectAndGroups(projectModel, task, projects, groups, inAll, type) && !CheckTag(task, tags, type))
					{
						return false;
					}
					goto IL_0138;
				}
			}
			goto IL_0100;
			IL_0138:
			if (!CheckPriority(task, priorities, type) || !CheckAssignTo(task, projectModel, assignTo, type))
			{
				return false;
			}
			if (!TaskUtils.CheckDate(task, startEndPairs, inTodayOrWeek, type, inCal, inCal || taskType == TaskType.Note))
			{
				return false;
			}
			if (checkProjectPermission && !projectModel.IsEnable())
			{
				return false;
			}
			return true;
			IL_0100:
			if (!CheckProjectAndGroups(projectModel, task, projects, groups, inAll, type) || !CheckTag(task, tags, type))
			{
				return false;
			}
			goto IL_0138;
		}).ToList();
		if (list.Count > 0 && !string.IsNullOrEmpty(keyword?.Trim()))
		{
			keyword = keyword.Trim().ToLower();
			List<string> list2 = keyword.Split(' ').ToList();
			list2.Remove("");
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (TaskBaseViewModel item in list)
			{
				if (dictionary.ContainsKey(item.Id))
				{
					continue;
				}
				string text = item.Title + "\n" + item.Content + "\n" + item.Desc;
				BlockingList<TaskBaseViewModel> checkItems = item.CheckItems;
				if (checkItems != null && checkItems.Count > 0)
				{
					text = item.CheckItems.Value.Aggregate(text, (string text2, TaskBaseViewModel check) => text2 + "\n" + check.Title);
				}
				List<AttachmentModel> attachmentsByTaskId = AttachmentCache.GetAttachmentsByTaskId(item.Id);
				if (attachmentsByTaskId != null && attachmentsByTaskId.Count > 0)
				{
					text = attachmentsByTaskId.Aggregate(text, (string text2, AttachmentModel attachment) => text2 + "\n[" + attachment.fileName + "]");
				}
				dictionary.Add(item.Id, text.ToLower());
			}
			HashSet<string> matchTaskIds = new HashSet<string>();
			foreach (string key in dictionary.Keys)
			{
				if (SearchHelper.KeyWordMatched(dictionary[key], keyword, list2))
				{
					matchTaskIds.Add(key);
				}
			}
			list = list.Where((TaskBaseViewModel t) => (type != LogicType.Not) ? matchTaskIds.Contains(t.Id) : (!matchTaskIds.Contains(t.Id))).ToList();
		}
		if (withChild)
		{
			List<TaskBaseViewModel> pinCheckChildren = TaskCache.GetPinCheckChildren(list.Select((TaskBaseViewModel t) => t.Id).ToList());
			list.AddRange(pinCheckChildren);
		}
		return list;
	}

	public static List<TaskBaseViewModel> GetTasksInArrange(ProjectExtra filter)
	{
		List<TaskBaseViewModel> source = TaskCache.LocalTaskViewModels.Values.ToList();
		List<string> projectIds = filter.ProjectIds;
		if (projectIds == null || projectIds.Count <= 0)
		{
			List<string> groupIds = filter.GroupIds;
			if ((groupIds == null || groupIds.Count <= 0) && LocalSettings.Settings.ArrangeDisplayType == 0)
			{
				goto IL_0099;
			}
		}
		if (LocalSettings.Settings.ArrangeDisplayType == 1)
		{
			List<string> tags = filter.Tags;
			if (tags == null || tags.Count <= 0)
			{
				goto IL_0099;
			}
		}
		int num = ((LocalSettings.Settings.ArrangeDisplayType == 2) ? 1 : 0);
		goto IL_009a;
		IL_009a:
		bool checkInAll = (byte)num != 0;
		return source.Where(delegate(TaskBaseViewModel task)
		{
			if (!CheckTask(task, showComplete: false))
			{
				return false;
			}
			if (LocalSettings.Settings.ArrangeTaskDateType == 1)
			{
				if (!task.StartDate.HasValue)
				{
					return false;
				}
				if ((task.DueDate.HasValue && task.DueDate.Value > DateTime.Today) || (!task.DueDate.HasValue && task.StartDate >= DateTime.Today))
				{
					return false;
				}
			}
			else if (task.StartDate.HasValue)
			{
				return false;
			}
			ProjectModel projectById = CacheManager.GetProjectById(task.ProjectId);
			if (projectById == null)
			{
				return false;
			}
			if (!projectById.IsEnable())
			{
				return false;
			}
			return CheckProjectAndGroups(projectById, task, filter.ProjectIds, filter.GroupIds, checkInAll) && CheckTag(task, filter.Tags);
		}).ToList();
		IL_0099:
		num = 1;
		goto IL_009a;
	}

	public static List<TaskBaseViewModel> GetTaskInSummary(SummaryFilterModel filter)
	{
		List<TaskBaseViewModel> list = TaskCache.LocalTaskViewModels.Values.ToList().Where(delegate(TaskBaseViewModel task)
		{
			if (!CheckTask(task))
			{
				return false;
			}
			if (task.Kind == "NOTE")
			{
				return false;
			}
			ProjectModel projectById = CacheManager.GetProjectById(task.ProjectId);
			if (projectById == null)
			{
				return false;
			}
			if (!CheckProjectAndGroups(projectById, task, filter.SelectedProjectIds, filter.SelectedProjectGroupIds, checkInAll: false, LogicType.Or, forceAssignToMe: true) || !CheckTag(task, filter.SelectedTags) || !CheckAssignTo(task, projectById, filter.Assigns))
			{
				return false;
			}
			if (filter.SelectedPriorities != null && filter.SelectedPriorities.Count > 0 && !filter.SelectedPriorities.Contains(task.Priority.ToString()))
			{
				return false;
			}
			if (filter.SelectedStatus != null && filter.SelectedStatus.Count > 0 && !filter.SelectedStatus.Contains("all"))
			{
				string item = "uncompleted";
				switch (task.Status)
				{
				case 0:
					item = ((task.Progress <= 0) ? "uncompleted" : "inProgress");
					break;
				case -1:
					item = "wontDo";
					break;
				case 1:
				case 2:
					item = "completed";
					break;
				}
				if (!filter.SelectedStatus.Contains(item))
				{
					return false;
				}
			}
			if (filter.StartDate.HasValue && filter.EndDate.HasValue)
			{
				DateTime dateTime = filter.EndDate.Value.AddDays(1.0);
				if (task.Status != 0 && task.CompletedTime.HasValue && task.CompletedTime.Value >= filter.StartDate.Value && task.CompletedTime.Value < dateTime)
				{
					return true;
				}
				if (task.StartDate.HasValue && ((task.StartDate.Value >= filter.StartDate.Value && task.StartDate.Value < dateTime) || (task.DueDate.HasValue && ((task.DueDate.Value > filter.StartDate.Value && task.DueDate.Value < dateTime) || (task.DueDate.Value >= dateTime && task.StartDate.Value < filter.StartDate.Value)))))
				{
					return true;
				}
				return false;
			}
			return true;
		}).ToList();
		Dictionary<string, Node<TaskBaseViewModel>> taskTree = TaskCache.GetAllTaskTree();
		HashSet<string> taskIdSet = new HashSet<string>(list.Select((TaskBaseViewModel t) => t.Id).ToList());
		Dictionary<string, TaskBaseViewModel> extraTasks = new Dictionary<string, TaskBaseViewModel>();
		foreach (TaskBaseViewModel item2 in list.Where((TaskBaseViewModel task) => !string.IsNullOrEmpty(task.Id) && taskTree.ContainsKey(task.Id)))
		{
			Node<TaskBaseViewModel> node = taskTree[item2.Id];
			List<Node<TaskBaseViewModel>> children = node.Children;
			if (children == null || children.Count <= 0 || taskIdSet.Contains(node.Value.ParentId) || string.IsNullOrEmpty(node.Value.Id) || extraTasks.ContainsKey(node.Value.Id))
			{
				continue;
			}
			node.GetAllChildrenValue().ForEach(delegate(TaskBaseViewModel child)
			{
				if (!string.IsNullOrEmpty(child.Id) && !taskIdSet.Contains(child.Id) && child.Deleted == 0 && child.Status == 0 && !extraTasks.ContainsKey(child.Id))
				{
					extraTasks[child.Id] = child;
				}
			});
		}
		list.AddRange(extraTasks.Values);
		return list;
	}

	private static bool CheckPriority(TaskBaseViewModel task, List<int> priorities, LogicType type = LogicType.Or)
	{
		if (priorities != null && priorities.Count > 0)
		{
			bool flag = task.Kind != "NOTE" && priorities.Contains(task.Priority);
			if (type != LogicType.Not)
			{
				return flag;
			}
			return !flag;
		}
		return true;
	}

	private static bool CheckAssignTo(TaskBaseViewModel task, ProjectModel project, List<string> assignTo, LogicType type = LogicType.Or)
	{
		if (assignTo != null && assignTo.Count > 0)
		{
			bool flag = false;
			if (!string.IsNullOrEmpty(task.Assignee) && task.Assignee != "-1" && (assignTo.Contains(task.Assignee) || assignTo.Contains("anyone")))
			{
				flag = true;
			}
			if (assignTo.Contains("me") && task.Assignee == LocalSettings.Settings.LoginUserId && project.IsShareList())
			{
				flag = true;
			}
			if (project.IsShareList() && assignTo.Contains("other") && !string.IsNullOrEmpty(task.Assignee) && task.Assignee != "-1" && task.Assignee != LocalSettings.Settings.LoginUserId)
			{
				flag = true;
			}
			if (assignTo.Contains("noassignee") && (string.IsNullOrEmpty(task.Assignee) || task.Assignee == "-1"))
			{
				flag = true;
			}
			if (type != LogicType.Not)
			{
				return flag;
			}
			return !flag;
		}
		return true;
	}

	public static List<TaskBaseViewModel> GetClosedModels(DateTime? fromTime, DateTime toTime, List<string> projectIds, int limit, bool showCheckItem = false, int closedType = 0, bool onlyClosedByMe = false)
	{
		int num = closedType;
		IOrderedEnumerable<TaskBaseViewModel> orderedEnumerable = ((num != -1 && num != 2) ? (from t in TaskCache.LocalTaskViewModels.Values
			where t != null && t.Status != 0 && t.Deleted == 0 && (!onlyClosedByMe || t.CompletedUser == LocalSettings.Settings.LoginUserId)
			orderby t.CompletedTime descending
			select t) : (from t in TaskCache.LocalTaskViewModels.Values
			where t != null && t.Status == closedType && t.Deleted == 0 && (!onlyClosedByMe || t.CompletedUser == LocalSettings.Settings.LoginUserId)
			orderby t.CompletedTime descending
			select t));
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		DateTime now = DateTime.Now;
		foreach (TaskBaseViewModel item in orderedEnumerable)
		{
			ProjectModel projectById = CacheManager.GetProjectById(item.ProjectId);
			if (projectById == null || !projectById.IsValid())
			{
				continue;
			}
			List<string> list2 = projectIds;
			if ((list2 == null || list2.Count <= 0 || projectIds.Contains(item.ProjectId)) && (!fromTime.HasValue || !(item.CompletedTime < fromTime)) && !(item.CompletedTime > toTime))
			{
				list.Add(item);
				if (list.Count >= limit && item.CompletedTime.HasValue && (now - item.CompletedTime.Value).TotalSeconds >= 1.0)
				{
					break;
				}
			}
		}
		List<TaskBaseViewModel> list3 = list;
		if (showCheckItem && closedType != -1)
		{
			if (!fromTime.HasValue && list3.Count > 0)
			{
				fromTime = list3.LastOrDefault()?.CompletedTime;
			}
			IEnumerable<TaskBaseViewModel> collection = TaskDetailItemCache.LocalCheckItemViewModels.Values.ToList().Where(delegate(TaskBaseViewModel item)
			{
				if (Utils.IsEmptyDate(item.StartDate) || Utils.IsEmptyDate(item.CompletedTime))
				{
					return false;
				}
				if (string.IsNullOrEmpty(item.ParentId) || !TaskCache.LocalTaskViewModels.ContainsKey(item.ParentId))
				{
					return false;
				}
				TaskBaseViewModel taskBaseViewModel = TaskCache.LocalTaskViewModels[item.ParentId];
				if (taskBaseViewModel.Status != 0 || item.Status == 0 || Utils.IsEmptyDate(item.StartDate))
				{
					return false;
				}
				if (!CheckTask(taskBaseViewModel))
				{
					return false;
				}
				ProjectModel projectById2 = CacheManager.GetProjectById(taskBaseViewModel.ProjectId);
				if (projectById2 == null)
				{
					return false;
				}
				List<string> list4 = projectIds;
				if (list4 != null && list4.Count > 0 && !projectIds.Contains(projectById2.id))
				{
					return false;
				}
				return (!fromTime.HasValue || !(item.CompletedTime < fromTime)) && !(item.CompletedTime >= toTime);
			});
			list3.AddRange(collection);
		}
		return list3;
	}

	public static bool CheckTeamTrash(string taskTeamId, bool isPerson)
	{
		bool num = UserManager.IsTeamUser();
		string text = ((!num) ? "" : CacheManager.GetTeam()?.id);
		bool flag = !isPerson;
		if (num && (!flag || !(taskTeamId == text)))
		{
			if (!flag)
			{
				return taskTeamId != text;
			}
			return false;
		}
		return true;
	}

	public static List<TaskBaseViewModel> GetTrashModels(bool isPerson)
	{
		List<TaskBaseViewModel> source = TaskCache.LocalTaskViewModels.Values.Where((TaskBaseViewModel task) => task != null && task.Deleted == 1 && CheckTeamTrash(task.TeamId, isPerson)).ToList();
		if (TrashSyncService.HasRemoteSnapshot(isPerson))
		{
			HashSet<string> loadedTaskIds = TrashSyncService.GetLoadedTaskIds(isPerson);
			HashSet<string> pendingTaskIds = TrashSyncService.GetPendingTaskIds(isPerson);
			source = source.Where((TaskBaseViewModel task) => loadedTaskIds.Contains(task.Id) || pendingTaskIds.Contains(task.Id)).ToList();
		}
		source = source.OrderByDescending((TaskBaseViewModel t) => t.ModifiedTime).ToList();
		if (TrashSyncService.IsDrainOff(isPerson))
		{
			return source;
		}
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		int num = TrashSyncService.TrashPage(isPerson) * 50;
		foreach (TaskBaseViewModel item in source)
		{
			list.Add(item);
			if (list.Count >= num)
			{
				break;
			}
		}
		return list;
	}

	private static bool CheckTask(TaskBaseViewModel task, bool showComplete = true)
	{
		if (task == null || task.Deleted != 0)
		{
			return false;
		}
		if (!showComplete && task.Status != 0)
		{
			return false;
		}
		return true;
	}

	private static bool CheckProjectAndGroups(ProjectModel project, TaskBaseViewModel task, List<string> projects, List<string> groups, bool checkInAll, LogicType type = LogicType.Or, bool forceAssignToMe = false)
	{
		if (!project.IsValid())
		{
			return false;
		}
		if ((projects == null || projects.Count <= 0) && (groups == null || groups.Count <= 0) && checkInAll && !CheckShowInSmartList(project, task, forceAssignToMe))
		{
			return false;
		}
		if (CalendarUtils.ContainsAllListId(projects))
		{
			return type != LogicType.Not;
		}
		if ((projects != null && projects.Count > 0) || (groups != null && groups.Count > 0))
		{
			bool flag = (projects != null && projects.Count > 0 && projects.Contains(project.id)) || (groups != null && groups.Count > 0 && groups.Contains(project.groupId));
			if (type != LogicType.Or)
			{
				if (type == LogicType.Not && flag)
				{
					goto IL_00b4;
				}
			}
			else if (!flag)
			{
				goto IL_00b4;
			}
		}
		return true;
		IL_00b4:
		return false;
	}

	public static bool CheckShowInSmartList(ProjectModel project, TaskBaseViewModel task, bool forceAssignToMe = false)
	{
		if (task != null)
		{
			return CheckShowInSmartList(project, task.Assignee, forceAssignToMe);
		}
		return false;
	}

	public static bool CheckShowInSmartList(ProjectModel project, TaskModel task, bool forceAssignToMe = false)
	{
		if (task != null)
		{
			return CheckShowInSmartList(project, task.assignee, forceAssignToMe);
		}
		return false;
	}

	public static bool CheckShowInSmartList(ProjectModel project, string assignee, bool forceAssignToMe = false)
	{
		if (project == null || !project.IsValid())
		{
			return false;
		}
		bool flag = string.IsNullOrEmpty(assignee) || assignee == "-1";
		bool flag2 = assignee == LocalSettings.Settings.LoginUserId;
		return project.ShowTypeState switch
		{
			ShowReminderType.All => true, 
			ShowReminderType.ExceptOther => flag || flag2 || forceAssignToMe, 
			ShowReminderType.OnlySelf => flag2 || forceAssignToMe, 
			ShowReminderType.Never => false, 
			_ => false, 
		};
	}

	private static bool CheckTag(TaskBaseViewModel task, List<string> tags, LogicType type = LogicType.Or)
	{
		tags = TagSerializer.NormalizeNames(tags);
		List<string> taskTags = TagSerializer.NormalizeNames(task.Tags) ?? new List<string>();
		if (tags != null && tags.Count > 0)
		{
			switch (type)
			{
			case LogicType.And:
				if (tags.Contains("*withtags"))
				{
					tags.Remove("*withtags");
					if (taskTags.Count == 0)
					{
						return false;
					}
				}
				if (tags.Contains("!tag"))
				{
					tags.Remove("!tag");
					if (taskTags.Count > 0)
					{
						return false;
					}
				}
				if (tags.Any((string tag) => !taskTags.Contains(tag)))
				{
					return false;
				}
				break;
			case LogicType.Or:
				if (tags.Contains("*withtags") && taskTags.Count > 0)
				{
					return true;
				}
				if (tags.Contains("!tag") && taskTags.Count == 0)
				{
					return true;
				}
				if (tags.All((string tag) => !taskTags.Contains(tag)))
				{
					return false;
				}
				break;
			case LogicType.Not:
				if (tags.Contains("*withtags") && taskTags.Count > 0)
				{
					return false;
				}
				if (tags.Contains("!tag") && taskTags.Count == 0)
				{
					return false;
				}
				if (tags.Any((string tag) => taskTags.Contains(tag)))
				{
					return false;
				}
				break;
			}
		}
		return true;
	}

	public static async Task<List<SearchContent>> GetTaskSearchContents(SearchFilterModel filter, List<string> tags)
	{
		tags = TagSerializer.NormalizeNames(tags);
		Dictionary<string, string> dict = new Dictionary<string, string>();
		foreach (TaskBaseViewModel item in from t in TaskCache.GetAllTask()
			where t != null && t.Deleted == 0
			select t)
		{
			if ((filter.Status.HasValue && filter.Status.Value != item.Status) || !CacheManager.CheckProjectValid(item.ProjectId) || (HasSpecificSearchProjectFilter(filter) && !filter.ProjectIds.Contains(item.ProjectId)))
			{
				continue;
			}
			List<int> priorities = filter.Priorities;
			if ((priorities != null && priorities.Count > 0 && !filter.Priorities.Contains(item.Priority)) || (filter.TaskType == TaskType.Note && item.Kind != "NOTE") || (filter.TaskType == TaskType.Task && item.Kind == "NOTE"))
			{
				continue;
			}
			List<string> assignees = filter.Assignees;
			if (assignees != null && assignees.Count > 0)
			{
				ProjectModel projectById = CacheManager.GetProjectById(item.ProjectId);
				if (projectById == null || !CheckAssignTo(item, projectById, filter.Assignees))
				{
					continue;
				}
			}
			List<string> list = tags;
			if (list != null && list.Count > 0)
			{
				string[] tags2 = item.Tags;
				if (((tags2 != null) ? tags2.Length : 0) < tags.Count || item.Tags.Intersect(tags).Count() < tags.Count)
				{
					continue;
				}
			}
			if (MatchesSearchFilterTags(item.Tag, item.Tags, filter.Tags) && (!filter.Start.HasValue || !filter.End.HasValue || (item.StartDate.HasValue && !(item.StartDate >= filter.End) && (!item.DueDate.HasValue || !(item.DueDate <= filter.Start)) && (item.DueDate.HasValue || !(item.StartDate < filter.Start)))))
			{
				if (!string.IsNullOrEmpty(item.Id) && dict.ContainsKey(item.Id))
				{
					Dictionary<string, string> dictionary = dict;
					string id = item.Id;
					dictionary[id] = dictionary[id] + "\r\n" + item.Title?.ToLower() + "\r\n" + item.Content?.ToLower() + "\r\n" + item.Desc?.ToLower();
				}
				else
				{
					dict[item.Id] = "\r\n" + item.Title?.ToLower() + "\r\n" + item.Content?.ToLower() + "\r\n" + item.Desc?.ToLower();
				}
			}
		}
		foreach (TaskBaseViewModel allCheckItem in TaskDetailItemCache.GetAllCheckItems())
		{
			if (!string.IsNullOrEmpty(allCheckItem.ParentId) && dict.ContainsKey(allCheckItem.ParentId))
			{
				TaskBaseViewModel taskBaseViewModel = TaskCache.LocalTaskViewModels[allCheckItem.ParentId];
				if (taskBaseViewModel != null && taskBaseViewModel.Kind == "CHECKLIST")
				{
					Dictionary<string, string> dictionary = dict;
					string id = allCheckItem.ParentId;
					dictionary[id] = dictionary[id] + "\r\n" + allCheckItem.Title?.ToLower();
				}
			}
		}
		ConcurrentDictionary<string, List<CommentModel>> concurrentDictionary = await TaskCommentCache.GetTaskCommentDict();
		foreach (string item2 in dict.Keys.ToList())
		{
			if (!concurrentDictionary.ContainsKey(item2))
			{
				continue;
			}
			foreach (CommentModel item3 in concurrentDictionary[item2])
			{
				Dictionary<string, string> dictionary = dict;
				string id = item2;
				dictionary[id] = dictionary[id] + "\r\n" + item3.title?.ToLower();
			}
		}
		List<SearchContent> list2 = new List<SearchContent>();
		if (dict.Any())
		{
			foreach (KeyValuePair<string, string> item4 in dict)
			{
				list2.Add(new SearchContent
				{
					TaskId = item4.Key,
					Content = item4.Value
				});
			}
		}
		return list2;
	}

	public static async Task<List<TaskSearchModel>> GetRecentCreatedTasks(int limit, List<string> exceptIds = null)
	{
		return (from task in (from task in TaskCache.GetAllTask()
				where IsValidTask(task, exceptIds)
				orderby task.CreatedTime descending
				select task).Take(limit)
			select new TaskSearchModel
			{
				Content = task.Content,
				SourceModel = task
			}).ToList();
	}

	private static bool IsValidTask(TaskBaseViewModel task, List<string> exceptIds)
	{
		if (task == null || task.Deleted != 0 || !task.IsTask || task.Status != 0 || (exceptIds != null && exceptIds.Contains(task.Id)))
		{
			return false;
		}
		return IsProjectValid(task.ProjectId);
	}

	private static bool IsProjectValid(string projectId)
	{
		return CacheManager.GetProjectById(projectId)?.IsEnable() ?? false;
	}

	private static bool HasSpecificSearchProjectFilter(SearchFilterModel filter)
	{
		return filter.ProjectIds?.Any((string id) => !CalendarUtils.IsNotConcreteProjectFilterId(id)) ?? false;
	}

	private static bool MatchesSearchFilterTags(string rawTag, IEnumerable<string> taskTags, List<string> filterTags)
	{
		filterTags = TagSerializer.NormalizeNames(filterTags);
		if (filterTags == null || filterTags.Count == 0)
		{
			return true;
		}
		List<string> normalizedTaskTags = TagSerializer.NormalizeNames(taskTags) ?? new List<string>();
		if (filterTags.Contains("!tag") && (normalizedTaskTags.Count == 0 || string.IsNullOrEmpty(rawTag) || rawTag == "[]"))
		{
			return true;
		}
		if (filterTags.Contains("*withtags") && normalizedTaskTags.Count > 0)
		{
			return true;
		}
		return filterTags.Where((string tag) => tag != "!tag" && tag != "*withtags").Any((string tag) => normalizedTaskTags.Contains(tag));
	}

	public static async Task<List<TaskSearchModel>> GetTaskSearchModels(SearchFilterModel filter, List<string> tags)
	{
		tags = TagSerializer.NormalizeNames(tags);
		Dictionary<string, TaskSearchModel> dict = new Dictionary<string, TaskSearchModel>();
		Dictionary<string, string> dictionary = (from model in AttachmentCache.GetAllAttachments()
			where model.IsActive()
			group model by model.taskId).ToDictionaryEx((IGrouping<string, AttachmentModel> group) => group.Key, (IGrouping<string, AttachmentModel> group) => string.Join(", ", group.Select((AttachmentModel model) => model.fileName)));
		if (filter.Category != SearchCategory.CalendarEvent && (filter.Category != SearchCategory.None || !filter.HasCalendarFilterOnly()))
		{
			foreach (TaskBaseViewModel item2 in from t in TaskCache.GetAllTask()
				where t != null && t.Deleted == 0 && (filter.Kind == SearchKind.Normal || (t.Status == 0 && t.IsTask))
				select t)
			{
				if ((filter.ExcludeTaskIds != null && filter.ExcludeTaskIds.Count > 0 && (filter.ExcludeTaskIds.Contains(item2.Id) || !CacheManager.CheckProjectEnable(item2.ProjectId, inAll: false))) || (filter.Status.HasValue && filter.Status.Value != item2.Status) || !CacheManager.CheckProjectValid(item2.ProjectId) || (HasSpecificSearchProjectFilter(filter) && !filter.ProjectIds.Contains(item2.ProjectId)))
				{
					continue;
				}
				List<int> priorities = filter.Priorities;
				if ((priorities != null && priorities.Count > 0 && !filter.Priorities.Contains(item2.Priority)) || (filter.TaskType == TaskType.Note && item2.Kind != "NOTE") || (filter.TaskType == TaskType.Task && item2.Kind == "NOTE"))
				{
					continue;
				}
				List<string> assignees = filter.Assignees;
				if (assignees != null && assignees.Count > 0)
				{
					ProjectModel projectById = CacheManager.GetProjectById(item2.ProjectId);
					if (projectById == null || !CheckAssignTo(item2, projectById, filter.Assignees))
					{
						continue;
					}
				}
				List<string> list = tags;
				if (list != null && list.Count > 0)
				{
					string[] tags2 = item2.Tags;
					if (((tags2 != null) ? tags2.Length : 0) < tags.Count || item2.Tags.Intersect(tags).Count() < tags.Count)
					{
						continue;
					}
				}
				if (MatchesSearchFilterTags(item2.Tag, item2.Tags, filter.Tags) && (!filter.Start.HasValue || !filter.End.HasValue || (item2.StartDate.HasValue && !(item2.StartDate >= filter.End) && (!item2.DueDate.HasValue || !(item2.DueDate <= filter.Start)) && (item2.DueDate.HasValue || !(item2.StartDate < filter.Start)))))
				{
					string value = string.Empty;
					dictionary.TryGetValue(item2.Id, out value);
					if (!string.IsNullOrEmpty(item2.Id) && dict.ContainsKey(item2.Id))
					{
						TaskSearchModel taskSearchModel = dict[item2.Id];
						taskSearchModel.SourceModel = item2;
						taskSearchModel.Content = item2.Content;
						taskSearchModel.Attachment = value;
					}
					else
					{
						TaskSearchModel value2 = new TaskSearchModel
						{
							Content = ((filter.Kind == SearchKind.Normal) ? item2.Content : string.Empty),
							Attachment = value,
							SourceModel = item2
						};
						dict[item2.Id] = value2;
					}
				}
			}
		}
		if (filter.Kind == SearchKind.Normal)
		{
			foreach (TaskBaseViewModel allCheckItem in TaskDetailItemCache.GetAllCheckItems())
			{
				if (!string.IsNullOrEmpty(allCheckItem.ParentId) && dict.ContainsKey(allCheckItem.ParentId))
				{
					TaskBaseViewModel taskBaseViewModel = TaskCache.LocalTaskViewModels[allCheckItem.ParentId];
					if (taskBaseViewModel != null && taskBaseViewModel.Kind == "CHECKLIST")
					{
						TaskSearchModel taskSearchModel2 = dict[allCheckItem.ParentId];
						taskSearchModel2.CheckItems = taskSearchModel2.CheckItems + ((!string.IsNullOrEmpty(taskSearchModel2.CheckItems)) ? "\r\n" : "") + allCheckItem.Title?.ToLower();
					}
				}
			}
		}
		if (filter.Kind == SearchKind.Normal)
		{
			ConcurrentDictionary<string, List<CommentModel>> concurrentDictionary = await TaskCommentCache.GetTaskCommentDict();
			foreach (string item3 in dict.Keys.ToList())
			{
				TaskSearchModel taskSearchModel3 = dict[item3];
				if (!concurrentDictionary.ContainsKey(item3))
				{
					continue;
				}
				foreach (CommentModel item4 in concurrentDictionary[item3])
				{
					taskSearchModel3.Comment = taskSearchModel3.Comment + "\r\n" + item4.title?.ToLower();
				}
			}
		}
		List<TaskSearchModel> result = dict.Values.ToList();
		if (filter.Kind == SearchKind.Normal && (filter.Category == SearchCategory.CalendarEvent || (filter.Category == SearchCategory.None && (tags == null || tags.Count == 0) && filter.SearchEvent())))
		{
			List<CalendarEventModel> events = await CalendarEventDao.GetAllShowEvents();
			List<CalendarEventModel> skipEvents = events.Where((CalendarEventModel e) => e.OriginalStartTime.HasValue).ToList();
			List<string> list2 = await ArchivedDao.GetArchivedKeys();
			foreach (CalendarEventModel item5 in events)
			{
				if (!CheckCalendarFilter(item5, filter))
				{
					continue;
				}
				TaskSearchModel taskSearchModel4 = new TaskSearchModel
				{
					Content = item5.Content
				};
				if (!string.IsNullOrEmpty(item5.RepeatFlag))
				{
					DateTime? eventNextRepeat = CalendarEventDao.GetEventNextRepeat(item5, skipEvents, list2);
					if (eventNextRepeat.HasValue)
					{
						double? num = (item5.DueEnd - item5.DueStart)?.TotalMinutes;
						item5.DueStart = eventNextRepeat;
						if (num.HasValue)
						{
							item5.DueEnd = eventNextRepeat.Value.AddMinutes(num.Value);
						}
					}
				}
				else
				{
					string item = ArchivedEventDao.GenerateEventKey(new EventArchiveArgs(item5));
					if (list2.Contains(item))
					{
						continue;
					}
				}
				if ((!item5.DueEnd.HasValue || !(item5.DueEnd <= DateTime.Today)) && (item5.DueEnd.HasValue || !(item5.DueStart < DateTime.Today)) && (!filter.Start.HasValue || !filter.End.HasValue || (item5.DueStart.HasValue && !(item5.DueStart >= filter.End) && (!item5.DueEnd.HasValue || !(item5.DueEnd <= filter.Start)) && (item5.DueEnd.HasValue || !(item5.DueStart < filter.Start)))))
				{
					taskSearchModel4.SourceModel = new TaskBaseViewModel(item5);
					result.Add(taskSearchModel4);
				}
			}
		}
		return result;
	}

	private static bool CheckCalendarFilter(CalendarEventModel eve, SearchFilterModel filter)
	{
		List<string> calendarIds = filter.CalendarIds;
		List<string> calendarAccountIds = filter.CalendarAccountIds;
		if ((calendarIds == null || calendarIds.Count == 0) && (calendarAccountIds == null || calendarAccountIds.Count == 0))
		{
			return true;
		}
		if (calendarIds != null && calendarIds.Contains("#allSubscribe"))
		{
			return true;
		}
		if (CalendarUtils.ContainsCalendarId(calendarIds, eve.CalendarId))
		{
			return true;
		}
		if (calendarAccountIds != null && calendarAccountIds.Count > 0)
		{
			BindCalendarModel bindCalendarModel = CacheManager.GetBindCalendars().FirstOrDefault((BindCalendarModel cal) => CalendarUtils.IsSameCalendarId(cal.Id, eve.CalendarId));
			if (bindCalendarModel != null)
			{
				return CalendarUtils.ContainsAccountId(calendarAccountIds, bindCalendarModel.AccountId);
			}
			return false;
		}
		return false;
	}

	public static List<TaskBaseViewModel> GetMatchedItems(ProjectIdentity projectIdentity, List<string> ids)
	{
		List<TaskBaseViewModel> checkItemByIds = TaskDetailItemCache.GetCheckItemByIds(ids);
		List<TaskBaseViewModel> result = null;
		if (!(projectIdentity is TodayProjectIdentity))
		{
			if (!(projectIdentity is TomorrowProjectIdentity))
			{
				if (!(projectIdentity is WeekProjectIdentity))
				{
					if (!(projectIdentity is FilterProjectIdentity filterProjectIdentity))
					{
						if (projectIdentity is MatrixQuadrantIdentity matrixQuadrantIdentity)
						{
							result = TaskService.GetItemMatchedNormalFilter(Parser.ToNormalModel(matrixQuadrantIdentity.Quadrant.rule), checkItemByIds);
						}
					}
					else
					{
						result = TaskService.GetCheckItemsMatchedFilter(filterProjectIdentity.Filter, checkItemByIds);
					}
				}
				else
				{
					result = GetItems(checkItemByIds, null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
					}, showComplete: true, inTodayOrWeek: true).ToList();
				}
			}
			else
			{
				result = GetItems(checkItemByIds, null, null, null, new List<FilterDatePair>
				{
					new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
				}).ToList();
			}
		}
		else
		{
			result = GetItems(checkItemByIds, null, null, null, new List<FilterDatePair>
			{
				new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
			}, showComplete: true, inTodayOrWeek: true).ToList();
		}
		return result;
	}

	public static List<TaskBaseViewModel> GetMatchedTasks(ProjectIdentity projectIdentity, List<string> ids)
	{
		List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(ids);
		return GetMatchedTasks(projectIdentity, tasksByIds);
	}

	public static List<TaskBaseViewModel> GetMatchedItemsInCal(DateTime startDate, DateTime endDate, List<string> ids, bool inParallel)
	{
		if (!LocalSettings.Settings.ShowCheckListInCal)
		{
			return new List<TaskBaseViewModel>();
		}
		bool showCompletedInCal = LocalSettings.Settings.ShowCompletedInCal;
		List<TaskBaseViewModel> checkItemByIds = TaskDetailItemCache.GetCheckItemByIds(ids);
		ProjectExtra projectFilter = (inParallel ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (projectFilter.FilterIds.Any())
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0]);
			if (filterModel != null)
			{
				return TaskService.GetCheckItemsMatchedFilter(filterModel, checkItemByIds)?.Where(delegate(TaskBaseViewModel t)
				{
					if (t.StartDate.HasValue)
					{
						DateTime? startDate2 = t.StartDate;
						DateTime dateTime = startDate;
						if (startDate2.HasValue && startDate2.GetValueOrDefault() >= dateTime && t.StartDate < endDate)
						{
							goto IL_01c9;
						}
					}
					if (t.DueDate.HasValue)
					{
						DateTime? startDate2 = t.DueDate;
						DateTime dateTime = startDate;
						if (startDate2.HasValue && startDate2.GetValueOrDefault() > dateTime && t.DueDate < endDate)
						{
							goto IL_01c9;
						}
					}
					if (t.StartDate.HasValue && t.DueDate.HasValue)
					{
						DateTime? startDate2 = t.StartDate;
						DateTime dateTime = startDate;
						if (startDate2.HasValue && startDate2.GetValueOrDefault() < dateTime && t.DueDate >= endDate)
						{
							goto IL_01c9;
						}
					}
					if (!t.StartDate.HasValue && !t.DueDate.HasValue && t.CompletedTime.HasValue && t.Status != 0 && t.CompletedTime >= startDate)
					{
						return t.CompletedTime < endDate;
					}
					return false;
					IL_01c9:
					return true;
				}).ToList();
			}
			return new List<TaskBaseViewModel>();
		}
		if ((projectFilter.SubscribeCalendars.Any() || projectFilter.BindAccounts.Any()) && projectFilter.GroupIds.Count == 0 && projectFilter.ProjectIds.Count == 0)
		{
			projectFilter.ProjectIds.Add("none");
		}
		List<FilterDatePair> startEndPairs = new List<FilterDatePair>
		{
			new FilterDatePair(startDate, endDate)
		};
		return GetItems(checkItemByIds, projectFilter.GroupIds, projectFilter.ProjectIds, projectFilter.Tags, startEndPairs, showCompletedInCal, inTodayOrWeek: false, projectFilter.GroupIds.Count == 0 && projectFilter.ProjectIds.Count == 0 && projectFilter.Tags.Count == 0, null, null, LogicType.Or, orTag: true).ToList();
	}

	public static async Task<List<TaskBaseViewModel>> GetMatchedTasksInCal(DateTime startDate, DateTime endDate, List<string> ids, bool inParallel)
	{
		bool showCompletedInCal = LocalSettings.Settings.ShowCompletedInCal;
		bool showCheckListInCal = LocalSettings.Settings.ShowCheckListInCal;
		List<TaskBaseViewModel> tasksByIds = TaskCache.GetTasksByIds(ids);
		List<TaskBaseViewModel> repeats = new List<TaskBaseViewModel>();
		foreach (TaskBaseViewModel task in tasksByIds)
		{
			if (!task.StartDate.HasValue || string.IsNullOrEmpty(task.RepeatFlag))
			{
				continue;
			}
			double diffMinutes = 0.0;
			if (task.DueDate.HasValue)
			{
				diffMinutes = (task.DueDate.Value - task.StartDate.Value).TotalMinutes;
			}
			string timeZone = (((!task.IsAllDay.HasValue || !task.IsAllDay.Value) && !task.IsFloating && task.TimeZoneName != TimeZoneData.LocalTimeZoneModel?.TimeZoneName) ? task.TimeZoneName : TimeZoneData.LocalTimeZoneModel?.TimeZoneName);
			RepeatDao.GetRepeatFlagDate(task, startDate.AddDays(-2 - (int)diffMinutes / 1440), endDate.AddDays(1.0)).ForEach(delegate(DateTime date)
			{
				DateTime? dateTime = null;
				if (task.DueDate.HasValue)
				{
					dateTime = TimeZoneUtils.ToLocalTime(TimeZoneUtils.LocalToTargetTzTime(date, timeZone).AddMinutes(diffMinutes), timeZone);
				}
				if (!(date >= startDate) || !(date < endDate))
				{
					DateTime? dateTime2 = dateTime;
					DateTime dateTime3 = startDate;
					if (!dateTime2.HasValue || !(dateTime2.GetValueOrDefault() > dateTime3) || !(dateTime <= endDate))
					{
						dateTime2 = dateTime;
						dateTime3 = endDate;
						if (!dateTime2.HasValue || !(dateTime2.GetValueOrDefault() > dateTime3) || !(date < startDate))
						{
							return;
						}
					}
				}
				double totalDays = (date - (task.StartDate ?? date)).TotalDays;
				if (totalDays != 0.0)
				{
					TaskBaseViewModel taskBaseViewModel = task.Copy();
					taskBaseViewModel.StartDate = taskBaseViewModel.StartDate?.AddDays(totalDays);
					taskBaseViewModel.DueDate = taskBaseViewModel.DueDate?.AddDays(totalDays);
					repeats.Add(taskBaseViewModel);
				}
			});
		}
		tasksByIds.AddRange(repeats);
		List<TaskBaseViewModel> checkItemsInTaskIds = TaskDetailItemCache.GetCheckItemsInTaskIds(tasksByIds.Select((TaskBaseViewModel t) => t.Id).ToList());
		ProjectExtra projectFilter = (inParallel ? LocalSettings.Settings.ExtraSettings.CalendarParallelSelectProject : ProjectExtra.Deserialize(LocalSettings.Settings.CalendarFilterData));
		if (projectFilter.FilterIds.Any())
		{
			FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.id == projectFilter.FilterIds[0]);
			if (filterModel != null)
			{
				List<TaskBaseViewModel> obj = TaskService.GetTasksMatchedFilter(filterModel, tasksByIds) ?? new List<TaskBaseViewModel>();
				List<TaskBaseViewModel> checkItemsMatchedFilter = TaskService.GetCheckItemsMatchedFilter(filterModel, checkItemsInTaskIds);
				obj.AddRange(checkItemsMatchedFilter);
				return obj.Where(delegate(TaskBaseViewModel t)
				{
					if (t.StartDate.HasValue)
					{
						DateTime? startDate2 = t.StartDate;
						DateTime dateTime = startDate;
						if (startDate2.HasValue && startDate2.GetValueOrDefault() >= dateTime && t.StartDate < endDate)
						{
							goto IL_01c9;
						}
					}
					if (t.DueDate.HasValue)
					{
						DateTime? startDate2 = t.DueDate;
						DateTime dateTime = startDate;
						if (startDate2.HasValue && startDate2.GetValueOrDefault() > dateTime && t.DueDate < endDate)
						{
							goto IL_01c9;
						}
					}
					if (t.StartDate.HasValue && t.DueDate.HasValue)
					{
						DateTime? startDate2 = t.StartDate;
						DateTime dateTime = startDate;
						if (startDate2.HasValue && startDate2.GetValueOrDefault() < dateTime && t.DueDate >= endDate)
						{
							goto IL_01c9;
						}
					}
					if (!t.StartDate.HasValue && !t.DueDate.HasValue && t.CompletedTime.HasValue && t.Status != 0 && t.CompletedTime >= startDate)
					{
						return t.CompletedTime < endDate;
					}
					return false;
					IL_01c9:
					return true;
				}).ToList();
			}
			return new List<TaskBaseViewModel>();
		}
		if ((projectFilter.SubscribeCalendars.Any() || projectFilter.BindAccounts.Any()) && projectFilter.GroupIds.Count == 0 && projectFilter.ProjectIds.Count == 0)
		{
			projectFilter.ProjectIds.Add("none");
		}
		List<FilterDatePair> startEndPairs = new List<FilterDatePair>
		{
			new FilterDatePair(startDate, endDate)
		};
		List<TaskBaseViewModel> list = GetTasks(tasksByIds, projectFilter.GroupIds, projectFilter.ProjectIds, projectFilter.Tags, startEndPairs, showCompletedInCal, showCheckListInCal, projectFilter.GroupIds.Count == 0 && projectFilter.ProjectIds.Count == 0 && projectFilter.Tags.Count == 0, null, null, LogicType.Or, withChild: false, inCal: true, orTag: true)?.ToList() ?? new List<TaskBaseViewModel>();
		if (showCheckListInCal)
		{
			IEnumerable<TaskBaseViewModel> items = GetItems(checkItemsInTaskIds, projectFilter.GroupIds, projectFilter.ProjectIds, projectFilter.Tags, startEndPairs, showCompletedInCal, inTodayOrWeek: false, projectFilter.GroupIds.Count == 0 && projectFilter.ProjectIds.Count == 0 && projectFilter.Tags.Count == 0, null, null, LogicType.Or, orTag: true);
			list.AddRange(items);
		}
		return list;
	}

	public static List<TaskBaseViewModel> GetMatchedTasks(ProjectIdentity projectIdentity, List<TaskBaseViewModel> tasks, bool checkCheckItem = false)
	{
		if (tasks == null || tasks.Count == 0)
		{
			return null;
		}
		List<TaskBaseViewModel> list = null;
		List<TaskBaseViewModel> list2 = null;
		if (!(projectIdentity is AllProjectIdentity))
		{
			if (!(projectIdentity is TodayProjectIdentity))
			{
				if (!(projectIdentity is TomorrowProjectIdentity))
				{
					if (!(projectIdentity is WeekProjectIdentity))
					{
						if (!(projectIdentity is GroupProjectIdentity groupProjectIdentity))
						{
							if (!(projectIdentity is AssignToMeProjectIdentity))
							{
								TagProjectIdentity tagProjectIdentity = projectIdentity as TagProjectIdentity;
								if (tagProjectIdentity == null)
								{
									if (!(projectIdentity is FilterProjectIdentity filterProjectIdentity))
									{
										if (!(projectIdentity is MatrixQuadrantIdentity matrixQuadrantIdentity))
										{
											ColumnProjectIdentity columnProjectIdentity = projectIdentity as ColumnProjectIdentity;
											if (columnProjectIdentity == null)
											{
												NormalProjectIdentity normalProjectIdentity = projectIdentity as NormalProjectIdentity;
												if (normalProjectIdentity != null)
												{
													list = tasks.Where((TaskBaseViewModel p) => p.ProjectId == normalProjectIdentity.Id && p.Deleted == 0).ToList();
												}
											}
											else
											{
												list = tasks.Where((TaskBaseViewModel p) => p.ProjectId == columnProjectIdentity.GetProjectId() && p.ColumnId == columnProjectIdentity.Id && p.Deleted == 0).ToList();
											}
										}
										else
										{
											list = TaskService.GetTaskMatchedNormalFilter(Parser.ToNormalModel(matrixQuadrantIdentity.Quadrant.rule), tasks);
											if (checkCheckItem)
											{
												List<TaskBaseViewModel> checkItemsInTaskIds = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
												list2 = TaskService.GetItemMatchedNormalFilter(Parser.ToNormalModel(matrixQuadrantIdentity.Quadrant.rule), checkItemsInTaskIds);
											}
										}
									}
									else
									{
										list = TaskService.GetTasksMatchedFilter(filterProjectIdentity.Filter, tasks);
										if (checkCheckItem)
										{
											List<TaskBaseViewModel> checkItemsInTaskIds = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
											list2 = TaskService.GetCheckItemsMatchedFilter(filterProjectIdentity.Filter, checkItemsInTaskIds);
										}
									}
								}
								else
								{
									List<string> tags = (from t in CacheManager.GetTags()
										where t.name == tagProjectIdentity.Tag || t.parent == tagProjectIdentity.Tag
										select t.name).ToList();
									list = GetTasks(tasks, null, null, tags, null, !LocalSettings.Settings.HideComplete).ToList();
								}
							}
							else
							{
								list = GetTasks(tasks, null, null, null, null, showComplete: true, inTodayOrWeek: false, inAll: false, new List<string> { "me" }).ToList();
							}
						}
						else
						{
							list = GetTasks(tasks, new List<string> { groupProjectIdentity.GroupId }).ToList();
						}
					}
					else
					{
						list = GetTasks(tasks, null, null, null, new List<FilterDatePair>
						{
							new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
						}, showComplete: true, inTodayOrWeek: true).ToList();
						if (checkCheckItem)
						{
							List<TaskBaseViewModel> checkItemsInTaskIds = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
							list2 = GetItems(checkItemsInTaskIds, null, null, null, new List<FilterDatePair>
							{
								new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(7.0))
							}, showComplete: true, inTodayOrWeek: true).ToList();
						}
					}
				}
				else
				{
					list = GetTasks(tasks, null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
					}).ToList();
					if (checkCheckItem)
					{
						List<TaskBaseViewModel> checkItemsInTaskIds = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
						list2 = GetItems(checkItemsInTaskIds, null, null, null, new List<FilterDatePair>
						{
							new FilterDatePair(DateTime.Today.AddDays(1.0), DateTime.Today.AddDays(2.0))
						}).ToList();
					}
				}
			}
			else
			{
				list = GetTasks(tasks, null, null, null, new List<FilterDatePair>
				{
					new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
				}, showComplete: true, inTodayOrWeek: true).ToList();
				if (checkCheckItem)
				{
					List<TaskBaseViewModel> checkItemsInTaskIds = TaskDetailItemCache.GetCheckItemsInTaskIds(tasks.Select((TaskBaseViewModel t) => t.Id).ToList());
					list2 = GetItems(checkItemsInTaskIds, null, null, null, new List<FilterDatePair>
					{
						new FilterDatePair(DateTime.Today, DateTime.Today.AddDays(1.0))
					}, showComplete: true, inTodayOrWeek: true).ToList();
				}
			}
		}
		else
		{
			list = tasks.Where(delegate(TaskBaseViewModel t)
			{
				if (t == null || t.Deleted != 0)
				{
					return false;
				}
				return CheckShowInSmartList(CacheManager.GetProjectById(t.ProjectId), t) ? true : false;
			}).ToList();
		}
		if (list != null)
		{
			if (list2 != null)
			{
				list.AddRange(list2);
			}
			return list;
		}
		return list2;
	}

	public static void OnProjectChanged(ProjectModel project)
	{
		foreach (TaskBaseViewModel item in TaskCache.LocalTaskViewModels.Values.ToList())
		{
			if (item.ProjectId == project.id)
			{
				item.SetProject(project.id);
			}
		}
	}

	public static void OnProjectDeleted(ProjectModel project)
	{
		foreach (TaskBaseViewModel item in TaskCache.LocalTaskViewModels.Values.ToList())
		{
			if (item.ProjectId == project.id)
			{
				item.Color = string.Empty;
				item.ProjectName = string.Empty;
				item.TeamId = string.Empty;
			}
		}
	}

	public static void ClearModels()
	{
		TaskCache.Clear();
		TaskDetailItemCache.Clear();
	}
}
