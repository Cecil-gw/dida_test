using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Cache;

public static class TaskCache
{
	public static readonly ConcurrentDictionary<string, TaskBaseViewModel> LocalTaskViewModels = new ConcurrentDictionary<string, TaskBaseViewModel>();

	public static readonly ConcurrentDictionary<string, string> LastDefaultColumnIds = new ConcurrentDictionary<string, string>();

	private static int _num;

	private static bool? existedSubTasks = null;

	public static async Task<HashSet<string>> InitLocalTasks()
	{
		await SyncStatusDao.RefreshSetParentSyncStatusCache();
		HashSet<string> ignoreTasks = null;
		List<TaskModel> taskModels = await TaskDao.GetAllTask();
		if (ABTestManager.IsCleanTask() && LocalSettings.Settings.ExtraSettings.LastClearTaskTime < DateUtils.GetDateNum(DateTime.Today.AddDays(-7.0)))
		{
			LocalSettings.Settings.ExtraSettings.LastClearTaskTime = DateUtils.GetDateNum(DateTime.Today);
			List<string> syncStatusTaskIds = new HashSet<string>((await SyncStatusDao.GetAllSyncStatus()).Select((SyncStatusModel s) => s.EntityId)).ToList();
			DateTime time = DateTime.Today.AddDays(-180.0);
			List<TaskModel> list = taskModels.Where((TaskModel t) => !syncStatusTaskIds.Contains(t.id) && ((t.status != 0 && t.completedTime < time) || (t.deleted != 0 && t.modifiedTime < time))).ToList();
			ignoreTasks = new HashSet<string>(list.Select((TaskModel t) => t.id));
			TaskService.DeleteTasksFromDb(list);
			UtilLog.Info(LocalSettings.Settings.ExtraSettings.LastClearTaskTime + " ClearHistoryTaskCount " + list.Count);
			CompletionLoadStatusDao.ResetPullTime(time);
			taskModels.RemoveAll((TaskModel t) => ignoreTasks.Contains(t.id));
		}
		taskModels.ForEach(delegate(TaskModel task)
		{
			Insert(task, notify: false);
		});
		await AttachmentCache.ResetDictItems();
		return ignoreTasks;
	}

	public static void AddToDict(TaskBaseViewModel task, bool checkNum = true)
	{
		if (task.IsTaskOrNote && !string.IsNullOrEmpty(task.Id) && !LocalTaskViewModels.ContainsKey(task.Id))
		{
			if (!task.CreatedTime.HasValue)
			{
				task.CreatedTime = DateTime.Now;
			}
			LocalTaskViewModels[task.Id] = task;
		}
		if (checkNum)
		{
			ProjectAndTaskIdsCache.OnTaskChanged(task, CheckMatchedType.All);
		}
	}

	public static Dictionary<string, Node<TaskBaseViewModel>> GetAllTaskTree()
	{
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = ((IEnumerable<TaskBaseViewModel>)LocalTaskViewModels.Values.Where((TaskBaseViewModel t) => t.Deleted == 0).ToList()).ToDictionaryEx((Func<TaskBaseViewModel, string>)((TaskBaseViewModel task) => task.Id), (Func<TaskBaseViewModel, Node<TaskBaseViewModel>>)((TaskBaseViewModel task) => new TaskNode(task)));
		TaskNodeUtils.BuildNodeTree(dictionary, checkParentExist: true);
		return dictionary;
	}

	public static Dictionary<string, Node<TaskBaseViewModel>> GetTaskTreeInProject(string projectId, bool withCompleted = true)
	{
		return GetTaskNodeDict((from t in LocalTaskViewModels.Values.ToList()
			where t != null && t.ProjectId == projectId && t.Deleted == 0 && (withCompleted || t.Status == 0)
			select t).ToList());
	}

	public static Dictionary<string, Node<TaskBaseViewModel>> GetTaskNodeDict(List<TaskBaseViewModel> tasks, bool buildByChildIds = false)
	{
		IEnumerable<TaskBaseViewModel> enumerable = tasks.Where((TaskBaseViewModel t) => t.Kind == "NOTE" || t.IsCheckItem);
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = tasks.Where((TaskBaseViewModel t) => t.Kind != "NOTE" && !t.IsCheckItem).ToDictionaryEx((Func<TaskBaseViewModel, string>)((TaskBaseViewModel task) => task.Id), (Func<TaskBaseViewModel, Node<TaskBaseViewModel>>)((TaskBaseViewModel task) => new TaskNode(task)));
		TaskNodeUtils.BuildNodeTree(dictionary, checkParentExist: true, buildByChildIds);
		foreach (TaskBaseViewModel item in enumerable)
		{
			if (!string.IsNullOrEmpty(item.Id) && !dictionary.ContainsKey(item.Id))
			{
				dictionary[item.Id] = new TaskNode(item);
			}
		}
		return dictionary;
	}

	public static List<TaskBaseViewModel> GetRepeatTasks(bool inAll)
	{
		return LocalTaskViewModels.Values.Where((TaskBaseViewModel task) => task != null && task.Deleted == 0 && task.Status == 0 && CacheManager.CheckProjectValid(task.ProjectId, inAll) && task.Kind != "NOTE" && !string.IsNullOrEmpty(task.RepeatFlag)).ToList().ToList();
	}

	internal static List<TaskBaseViewModel> GetTaskInQuickLink()
	{
		return (from x in GetAllTask().Where(delegate(TaskBaseViewModel t)
			{
				ProjectModel projectById = CacheManager.GetProjectById(t.ProjectId);
				if (projectById == null || !projectById.IsValid())
				{
					return false;
				}
				return t.Deleted == 0 && !string.IsNullOrWhiteSpace(t.Title);
			})
			orderby x.Status == 0 descending
			select x).ThenBy((TaskBaseViewModel x) => x.Title, StringComparer.CurrentCulture).ToList();
	}

	public static List<TaskBaseViewModel> GetModelsByIds(List<string> ids)
	{
		if (ids == null || ids.Count == 0)
		{
			return new List<TaskBaseViewModel>();
		}
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		foreach (TaskBaseViewModel value in LocalTaskViewModels.Values)
		{
			if (value != null && value.Deleted == 0 && ids.Contains(value.Id) && CacheManager.CheckProjectValid(value.ProjectId))
			{
				list.Add(value);
			}
		}
		return list;
	}

	public static IEnumerable<TaskBaseViewModel> GetTasksInProject(string projectId)
	{
		return (from t in LocalTaskViewModels.Values.ToList()
			where t.ProjectId == projectId && t.Deleted == 0
			select t).ToList();
	}

	public static async Task<List<TaskBaseViewModel>> GetTasksInProjectOrColumn(string projectId, string columnId = null, bool isDefaultColumn = false)
	{
		Dictionary<string, Node<TaskBaseViewModel>> nodes = GetTaskTreeInProject(projectId);
		if (CacheManager.GetProjectById(projectId) == null)
		{
			return new List<TaskBaseViewModel>();
		}
		List<string> columnIds = null;
		if (isDefaultColumn)
		{
			columnIds = (await ColumnDao.GetColumnsByProjectId(projectId))?.Select((ColumnModel c) => c.id).ToList();
		}
		return (from t in nodes.Values.Where(delegate(Node<TaskBaseViewModel> node)
			{
				if (string.IsNullOrEmpty(columnId))
				{
					return true;
				}
				string text = node.Value.ParentId;
				TaskBaseViewModel taskBaseViewModel = null;
				HashSet<string> hashSet = new HashSet<string> { node.NodeId };
				while (!string.IsNullOrEmpty(text))
				{
					if (hashSet.Contains(text))
					{
						TaskDao.UpdateParent(text, "", needSync: false);
						break;
					}
					if (nodes.ContainsKey(text))
					{
						hashSet.Add(text);
						taskBaseViewModel = nodes[text].Value;
						text = taskBaseViewModel.ParentId;
					}
					else
					{
						text = "";
						taskBaseViewModel = null;
					}
				}
				string text2 = taskBaseViewModel?.ColumnId ?? node.Value.ColumnId;
				return text2 == columnId || (isDefaultColumn && (string.IsNullOrEmpty(text2) || !(columnIds?.Contains(text2) ?? false)));
			})
			select t.Value).ToList();
	}

	public static List<TaskBaseViewModel> GetModelsInProject(string projectId)
	{
		if (CacheManager.GetProjectById(projectId) == null)
		{
			return new List<TaskBaseViewModel>();
		}
		return (from t in GetTasksInProject(projectId)
			where t != null
			select t).ToList();
	}

	public static async Task<List<TaskBaseViewModel>> GetModelsInProjectColumn(string projectId, string columnId = null, bool isDefaultColumn = false)
	{
		if (CacheManager.GetProjectById(projectId) == null)
		{
			return new List<TaskBaseViewModel>();
		}
		return await GetTasksInProjectOrColumn(projectId, columnId, isDefaultColumn);
	}

	public static List<string> GetAllTaskIds()
	{
		return LocalTaskViewModels.Keys.ToList();
	}

	public static HashSet<string> GetAllTaskIdMap()
	{
		return new HashSet<string>(GetAllTaskIds());
	}

	public static TaskBaseViewModel GetTaskById(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId) && LocalTaskViewModels.TryGetValue(taskId, out var value))
		{
			return value;
		}
		return null;
	}

	public static TaskBaseViewModel SafeGetTaskViewModel(TaskModel task)
	{
		if (task == null)
		{
			UtilLog.Info("task is null");
			return new TaskBaseViewModel();
		}
		if (string.IsNullOrEmpty(task.id))
		{
			UtilLog.Info("taskId is null");
			return new TaskBaseViewModel(task);
		}
		return Insert(task, notify: false);
	}

	public static TaskBaseViewModel Insert(TaskModel task, bool notify = true)
	{
		if (!string.IsNullOrEmpty(task?.id) && !string.IsNullOrEmpty(task.projectId))
		{
			if (LocalTaskViewModels.TryGetValue(task.id, out var value))
			{
				if (notify)
				{
					value.SetProperties(new TaskBaseViewModel(task));
					ProjectAndTaskIdsCache.OnTaskChanged(value, CheckMatchedType.All);
				}
				return value;
			}
			TaskBaseViewModel taskBaseViewModel = new TaskBaseViewModel(task);
			if (LocalTaskViewModels.TryAdd(task.id, taskBaseViewModel))
			{
				if (notify && taskBaseViewModel.Status == 0)
				{
					ProjectAndTaskIdsCache.OnTaskChanged(taskBaseViewModel, CheckMatchedType.All);
				}
				return taskBaseViewModel;
			}
		}
		return new TaskBaseViewModel(task);
	}

	public static List<TaskBaseViewModel> InsertAll(List<TaskModel> tasks)
	{
		return (from t in tasks?.Select((TaskModel t) => Insert(t))
			where t != null
			select t).ToList();
	}

	public static void DeleteTask(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId) && LocalTaskViewModels.ContainsKey(taskId) && LocalTaskViewModels.TryRemove(taskId, out var value))
		{
			ProjectAndTaskIdsCache.OnTaskRemoved(value);
		}
	}

	public static List<long> GetTaskSortOrdersInProject(string projectId, string parentId)
	{
		return (from task in LocalTaskViewModels.Values
			where task.ProjectId == projectId && Utils.IsEqualString(parentId, task.ParentId) && task.ProjectId == projectId
			select task.SortOrder).ToList();
	}

	public static List<string> GetDeletedTaskIds()
	{
		return (from task in LocalTaskViewModels.Values
			where task.Deleted == 1
			select task.Id).ToList();
	}

	public static List<TaskBaseViewModel> GetAllTask()
	{
		return LocalTaskViewModels.Values.ToList();
	}

	public static List<TaskBaseViewModel> GetAllTask(Func<TaskBaseViewModel, bool> where)
	{
		return LocalTaskViewModels.Values.Where(where).ToList();
	}

	public static List<TaskDao.TitleModel> GetTaskTitleModels()
	{
		return (from task in LocalTaskViewModels.Values
			where task.Deleted != 2
			select new TaskDao.TitleModel
			{
				TaskId = task.Id,
				Title = task.Title,
				Kind = task.Kind
			}).ToList();
	}

	public static List<TaskBaseViewModel> GetPinCheckChildren(List<string> taskIds)
	{
		Dictionary<string, Node<TaskBaseViewModel>> taskTree = GetAllTaskTree();
		HashSet<string> taskIdSet = new HashSet<string>(taskIds);
		Dictionary<string, TaskBaseViewModel> extraTasks = new Dictionary<string, TaskBaseViewModel>();
		foreach (string item in taskIds.Where((string id) => !string.IsNullOrEmpty(id) && taskTree.ContainsKey(id)))
		{
			Node<TaskBaseViewModel> node = taskTree[item];
			bool isNodePinned = node.Value.PinnedTime > 0;
			List<Node<TaskBaseViewModel>> children = node.Children;
			if (children == null || children.Count <= 0 || taskIdSet.Contains(node.Value.ParentId) || string.IsNullOrEmpty(node.Value.Id) || extraTasks.ContainsKey(node.Value.Id))
			{
				continue;
			}
			List<string> failedIds = new List<string>();
			node.GetAllChildrenValue().ForEach(delegate(TaskBaseViewModel child)
			{
				if (!taskIdSet.Contains(child.Id) && !string.IsNullOrEmpty(child.Id) && child.Deleted == 0 && child.Status == 0 && !extraTasks.ContainsKey(child.Id))
				{
					if (isNodePinned == child.PinnedTime > 0 && !failedIds.Contains(child.ParentId))
					{
						extraTasks[child.Id] = child;
					}
					else
					{
						failedIds.Add(child.Id);
					}
				}
			});
		}
		return extraTasks.Values.ToList();
	}

	public static Node<TaskBaseViewModel> GetTaskNode(string id, string projectId)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		if (LocalTaskViewModels.TryGetValue(id, out var value) && value.Kind == "NOTE")
		{
			return new TaskNode(value);
		}
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = ((IEnumerable<TaskBaseViewModel>)(from t in LocalTaskViewModels.Values.ToList()
			where t.ProjectId == projectId && t.Deleted == 0
			select t).ToList()).ToDictionaryEx((Func<TaskBaseViewModel, string>)((TaskBaseViewModel task) => task.Id), (Func<TaskBaseViewModel, Node<TaskBaseViewModel>>)((TaskBaseViewModel task) => new TaskNode(task)));
		TaskNodeUtils.BuildNodeTree(dictionary);
		if (dictionary.ContainsKey(id))
		{
			return dictionary[id];
		}
		return null;
	}

	public static List<TaskBaseViewModel> GetAllSubTasksById(string id, string projectId)
	{
		return (from t in GetTaskNode(id, projectId)?.GetAllChildrenValue()
			where t != null
			select t).ToList();
	}

	public static bool CanTaskCompletedByCheckItem(string taskId)
	{
		return !(GetAllTask()?.Any((TaskBaseViewModel t) => t.Deleted == 0 && t.ParentId == taskId && t.Status == 0) ?? false);
	}

	public static List<TaskBaseViewModel> GetAllParentTasksById(string id)
	{
		List<TaskBaseViewModel> tasks = new List<TaskBaseViewModel>();
		if (!string.IsNullOrEmpty(id) && LocalTaskViewModels.ContainsKey(id))
		{
			GetParent(LocalTaskViewModels[id].ParentId);
		}
		return tasks;
		void GetParent(string taskId)
		{
			while (!string.IsNullOrEmpty(taskId) && LocalTaskViewModels.ContainsKey(taskId))
			{
				TaskBaseViewModel taskBaseViewModel = LocalTaskViewModels[taskId];
				tasks.Add(taskBaseViewModel);
				taskId = taskBaseViewModel.ParentId;
			}
		}
	}

	public static TaskBaseViewModel FindParentByTaskTree(string id, string projectId)
	{
		if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(projectId))
		{
			return null;
		}
		Dictionary<string, Node<TaskBaseViewModel>> taskNodeDict = GetTaskNodeDict((from t in LocalTaskViewModels.Values.ToList()
			where t.ProjectId == projectId && t.Deleted == 0
			select t).ToList(), buildByChildIds: true);
		if (!taskNodeDict.ContainsKey(id))
		{
			return null;
		}
		return taskNodeDict[id].Parent?.Value;
	}

	public static List<TaskBaseViewModel> GetTaskAndChildrenInBatch(List<string> taskIds, bool checkExist = true, string projectId = null)
	{
		if (taskIds == null || taskIds.Count == 0)
		{
			return new List<TaskBaseViewModel>();
		}
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = (string.IsNullOrEmpty(projectId) ? GetAllTaskTree() : GetTaskTreeInProject(projectId));
		if (checkExist)
		{
			Dictionary<string, TaskBaseViewModel> taskDict = new Dictionary<string, TaskBaseViewModel>();
			foreach (string taskId in taskIds)
			{
				if (string.IsNullOrEmpty(taskId) || !dictionary.ContainsKey(taskId))
				{
					continue;
				}
				Node<TaskBaseViewModel> node = dictionary[taskId];
				List<Node<TaskBaseViewModel>> children = node.Children;
				if (children != null && children.Count > 0 && (string.IsNullOrEmpty(node.Value.ParentId) || !taskDict.ContainsKey(node.Value.ParentId)) && !string.IsNullOrEmpty(node.Value.Id) && !taskDict.ContainsKey(node.Value.Id))
				{
					taskDict[node.Value.Id] = node.Value;
					node.GetAllChildrenValue()?.ForEach(delegate(TaskBaseViewModel child)
					{
						if (!string.IsNullOrEmpty(child?.Id) && !taskDict.ContainsKey(child.Id) && child.Deleted == 0)
						{
							taskDict[child.Id] = child;
						}
					});
				}
				else
				{
					taskDict[node.Value.Id] = node.Value;
				}
			}
			return taskDict.Values.ToList();
		}
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		foreach (string taskId2 in taskIds)
		{
			if (!string.IsNullOrEmpty(taskId2) && dictionary.ContainsKey(taskId2))
			{
				Node<TaskBaseViewModel> node2 = dictionary[taskId2];
				list.Add(node2.Value);
				list.AddRange(node2.GetAllChildrenValue());
			}
		}
		return list;
	}

	public static bool ExistTask(string id)
	{
		if (!string.IsNullOrEmpty(id))
		{
			return LocalTaskViewModels.ContainsKey(id);
		}
		return false;
	}

	public static void Clear()
	{
		LocalTaskViewModels.Clear();
	}

	public static bool IsParentTask(string taskId)
	{
		if (!string.IsNullOrEmpty(taskId))
		{
			return LocalTaskViewModels.Values.Any((TaskBaseViewModel t) => t.Deleted == 0 && t.ParentId == taskId);
		}
		return false;
	}

	public static bool ExistParentTask(List<string> taskIds)
	{
		return LocalTaskViewModels.Values.Any((TaskBaseViewModel t) => !string.IsNullOrEmpty(t.ParentId) && taskIds.Contains(t.ParentId) && t.Deleted == 0);
	}

	public static bool IsChildTask(string parentId, string projectId)
	{
		if (string.IsNullOrEmpty(parentId))
		{
			return false;
		}
		if (LocalTaskViewModels.ContainsKey(parentId))
		{
			TaskBaseViewModel taskBaseViewModel = LocalTaskViewModels[parentId];
			if (taskBaseViewModel.Deleted == 0)
			{
				return taskBaseViewModel.ProjectId == projectId;
			}
			return false;
		}
		return false;
	}

	public static int GetMaxTaskDepth(string taskId, string projectId)
	{
		List<TaskBaseViewModel> children = GetChildren(taskId, projectId);
		if (children == null || !children.Any())
		{
			return 1;
		}
		int num = 0;
		foreach (TaskBaseViewModel item in children)
		{
			int maxTaskDepth = GetMaxTaskDepth(item.Id, item.ProjectId);
			num = Math.Max(num, maxTaskDepth);
		}
		return num + 1;
	}

	public static int GetTaskLevel(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return 0;
		}
		if (LocalTaskViewModels.ContainsKey(taskId))
		{
			TaskBaseViewModel taskBaseViewModel = LocalTaskViewModels[taskId];
			int num = 0;
			HashSet<string> hashSet = new HashSet<string> { taskId };
			while (!string.IsNullOrEmpty(taskBaseViewModel.ParentId) && LocalTaskViewModels.ContainsKey(taskBaseViewModel.ParentId))
			{
				if (hashSet.Contains(taskBaseViewModel.ParentId))
				{
					TaskDao.UpdateParent(taskBaseViewModel.Id, "", needSync: false);
					UtilLog.Info("SetTaskParent empty " + taskBaseViewModel.Id + " Getlevel loop");
					break;
				}
				TaskBaseViewModel taskBaseViewModel2 = LocalTaskViewModels[taskBaseViewModel.ParentId];
				if (!(taskBaseViewModel2.ProjectId == taskBaseViewModel.ProjectId) || taskBaseViewModel2.Deleted != 0)
				{
					break;
				}
				hashSet.Add(taskBaseViewModel2.Id);
				taskBaseViewModel = taskBaseViewModel2;
				num++;
			}
			return num;
		}
		return 0;
	}

	public static TaskBaseViewModel GetTopParentByTaskId(string taskId)
	{
		if (LocalTaskViewModels.ContainsKey(taskId))
		{
			TaskBaseViewModel taskBaseViewModel = LocalTaskViewModels[taskId];
			HashSet<string> hashSet = new HashSet<string> { taskId };
			while (!string.IsNullOrEmpty(taskBaseViewModel.ParentId) && LocalTaskViewModels.ContainsKey(taskBaseViewModel.ParentId))
			{
				string parentId = taskBaseViewModel.ParentId;
				if (hashSet.Contains(parentId))
				{
					TaskDao.UpdateParent(taskBaseViewModel.Id, "", needSync: false);
					break;
				}
				TaskBaseViewModel taskBaseViewModel2 = LocalTaskViewModels[parentId];
				if (!(taskBaseViewModel2.ProjectId == taskBaseViewModel.ProjectId) || taskBaseViewModel2.Deleted != 0)
				{
					break;
				}
				taskBaseViewModel = taskBaseViewModel2;
				hashSet.Add(parentId);
			}
			return taskBaseViewModel;
		}
		return null;
	}

	public static List<TaskBaseViewModel> GetTasksByIds(List<string> taskIds)
	{
		List<TaskBaseViewModel> list = new List<TaskBaseViewModel>();
		if (taskIds == null)
		{
			return list;
		}
		foreach (string taskId in taskIds)
		{
			TaskBaseViewModel taskById = GetTaskById(taskId);
			if (taskById != null)
			{
				list.Add(taskById);
			}
		}
		return list;
	}

	public static List<TaskBaseViewModel> GetChildren(string id, string projectId)
	{
		return LocalTaskViewModels.Values.Where((TaskBaseViewModel t) => t != null && t.Deleted == 0 && t.ParentId == id && t.ProjectId == projectId).ToList();
	}

	public static bool ExistSubTasks()
	{
		if (existedSubTasks == true)
		{
			return true;
		}
		if (!existedSubTasks.HasValue)
		{
			bool num = GetAllTask().Exists((TaskBaseViewModel it) => it.Deleted == 0 && !string.IsNullOrEmpty(it.ParentId));
			if (num)
			{
				existedSubTasks = true;
			}
			return num;
		}
		return false;
	}

	public static bool ExistParent(string id, string parentId)
	{
		bool flag = ExistTask(id);
		string text = id;
		HashSet<string> hashSet = new HashSet<string>();
		while (flag)
		{
			TaskBaseViewModel taskBaseViewModel = LocalTaskViewModels[text];
			hashSet.Add(text);
			text = taskBaseViewModel.ParentId;
			if (text == parentId)
			{
				return true;
			}
			flag = !hashSet.Contains(taskBaseViewModel.ParentId) && ExistTask(taskBaseViewModel.ParentId);
		}
		return false;
	}

	public static List<TaskBaseViewModel> GetAllOutDateTasks()
	{
		return LocalTaskViewModels.Values.ToList().Where(delegate(TaskBaseViewModel t)
		{
			if (!t.Editable || t.Status != 0 || t.Deleted != 0 || t.IsNote)
			{
				return false;
			}
			return TaskViewModelHelper.CheckShowInSmartList(CacheManager.GetProjectById(t.ProjectId), t) && t.OutDate();
		}).ToList();
	}

	public static bool FindParent(string taskId, string parentId)
	{
		if (!string.IsNullOrEmpty(taskId) && !string.IsNullOrEmpty(parentId) && LocalTaskViewModels.ContainsKey(taskId))
		{
			TaskBaseViewModel taskBaseViewModel = LocalTaskViewModels[taskId];
			HashSet<string> hashSet = new HashSet<string> { taskId };
			while (!string.IsNullOrEmpty(taskBaseViewModel.ParentId) && LocalTaskViewModels.ContainsKey(taskBaseViewModel.ParentId))
			{
				string parentId2 = taskBaseViewModel.ParentId;
				if (parentId2 == parentId)
				{
					return true;
				}
				if (hashSet.Contains(parentId2))
				{
					TaskDao.UpdateParent(taskBaseViewModel.Id, "", needSync: false);
					UtilLog.Info("SetTaskParent empty " + taskBaseViewModel.Id + " FindParent loop");
					break;
				}
				TaskBaseViewModel taskBaseViewModel2 = LocalTaskViewModels[parentId2];
				if (!(taskBaseViewModel2.ProjectId == taskBaseViewModel.ProjectId) || taskBaseViewModel2.Deleted != 0)
				{
					break;
				}
				taskBaseViewModel = taskBaseViewModel2;
				hashSet.Add(parentId2);
			}
			return false;
		}
		return false;
	}

	public static List<TaskBaseViewModel> GetEmptyColumnTaskInProject(string projectId)
	{
		return (from t in LocalTaskViewModels.Values.ToList()
			where t.ProjectId == projectId && t.Deleted == 0 && t.Status == 0 && string.IsNullOrEmpty(t.ColumnId)
			select t).ToList();
	}

	public static string GetLastDefaultColumnId(string projectId)
	{
		LastDefaultColumnIds.TryGetValue(projectId, out var value);
		return value;
	}

	public static void SetLastDefaultColumnId(string projectId, string columnId)
	{
		LastDefaultColumnIds.AddOrUpdate(projectId, columnId, (string key, string oldValue) => columnId);
	}

	public static List<TaskBaseViewModel> GetInvalidColumnTasksInProject(string projectId, List<string> validColumnIds)
	{
		HashSet<string> validSet = new HashSet<string>(validColumnIds ?? new List<string>());
		return (from t in LocalTaskViewModels.Values.ToList()
			where t.ProjectId == projectId && t.Deleted == 0 && t.Status == 0 && (string.IsNullOrEmpty(t.ColumnId) || !validSet.Contains(t.ColumnId))
			select t).ToList();
	}

	public static List<string> GetAllTaskAndSubTaskIdsByIds(List<string> ids)
	{
		HashSet<string> hashSet = new HashSet<string>(ids);
		foreach (TaskBaseViewModel item in LocalTaskViewModels.Values.ToList())
		{
			if (hashSet.Contains(item.Id) || string.IsNullOrEmpty(item.Id) || string.IsNullOrEmpty(item.ParentId) || item.Deleted == 2)
			{
				continue;
			}
			TaskBaseViewModel taskBaseViewModel = item;
			HashSet<string> hashSet2 = new HashSet<string> { item.Id };
			while (!string.IsNullOrEmpty(taskBaseViewModel.ParentId) && LocalTaskViewModels.ContainsKey(taskBaseViewModel.ParentId))
			{
				string parentId = taskBaseViewModel.ParentId;
				if (hashSet.Contains(parentId))
				{
					hashSet.Add(item.Id);
					break;
				}
				if (hashSet2.Contains(parentId))
				{
					TaskDao.UpdateParent(taskBaseViewModel.Id, "", needSync: false);
					UtilLog.Info("SetTaskParent empty " + taskBaseViewModel.Id + " FindParent loop");
					break;
				}
				TaskBaseViewModel taskBaseViewModel2 = LocalTaskViewModels[parentId];
				if (taskBaseViewModel2.Deleted == 2 || !(taskBaseViewModel2.ProjectId == taskBaseViewModel.ProjectId))
				{
					break;
				}
				taskBaseViewModel = taskBaseViewModel2;
				hashSet2.Add(parentId);
			}
		}
		return hashSet.ToList();
	}

	public static DateTime GetEarliestTime()
	{
		return LocalTaskViewModels.Values.ToList().Min((TaskBaseViewModel t) => t.StartDate ?? t.CompletedTime ?? DateTime.Today);
	}
}
