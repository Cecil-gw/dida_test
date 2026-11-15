using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Util;

public static class TaskNodeUtils
{
	public static List<Node<DisplayItemModel>> GetSortedTaskNodeTree(IEnumerable<DisplayItemModel> models, bool buildByChildIds = false)
	{
		Dictionary<string, Node<DisplayItemModel>> dictionary = new Dictionary<string, Node<DisplayItemModel>>();
		if (models == null)
		{
			return null;
		}
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		List<DisplayItemModel> list = models.ToList();
		List<string> noteList = new List<string>();
		for (int i = 0; i < list.Count; i++)
		{
			DisplayItemModel displayItemModel = list[i];
			if (displayItemModel.Kind == "NOTE")
			{
				if (displayItemModel.Parent != null)
				{
					displayItemModel.Parent = null;
					TaskDao.UpdateParent(displayItemModel.Id, string.Empty, needSync: false).ContinueWith(UtilRun.LogTask);
					UtilLog.Info("SetTaskParent empty " + displayItemModel.Id + " is Note");
				}
				displayItemModel.Children = null;
				noteList.Add(list[i].Id);
			}
			dictionary2[list[i].Id] = i;
			dictionary[list[i].Id] = new DisplayItemNode(list[i]);
		}
		foreach (Node<DisplayItemModel> item in from item in dictionary.Values
			where noteList.Contains(item.ParentId)
			where !string.IsNullOrEmpty(item.ParentId)
			select item)
		{
			item.Parent = null;
			item.ParentId = string.Empty;
		}
		BuildNodeTree(dictionary, checkParentExist: false, buildByChildIds);
		List<Node<DisplayItemModel>> list2 = dictionary.Values.Where((Node<DisplayItemModel> node) => !node.HasParent).ToList();
		SortNodes(list2, dictionary2);
		return list2;
	}

	public static List<TaskBaseViewModel> SortTaskDisplayModels(IEnumerable<TaskBaseViewModel> models)
	{
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = new Dictionary<string, Node<TaskBaseViewModel>>();
		if (models == null)
		{
			return null;
		}
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		List<TaskBaseViewModel> list = models.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			dictionary2[list[i].Id] = i;
			dictionary[list[i].Id] = new TaskNode(list[i]);
		}
		BuildNodeTree(dictionary);
		List<Node<TaskBaseViewModel>> list2 = dictionary.Values.Where((Node<TaskBaseViewModel> node) => !node.HasParent).ToList();
		SortNodes(list2, dictionary2);
		List<TaskBaseViewModel> list3 = new List<TaskBaseViewModel>();
		foreach (Node<TaskBaseViewModel> item in list2)
		{
			list3.Add(item.Value);
			list3.AddRange(from n in item.GetAllChildrenNode()
				select n.Value);
		}
		return list3;
	}

	public static void BuildNodeTree<T>(Dictionary<string, Node<T>> taskNodes, bool checkParentExist = false, bool buildByChildIds = false) where T : INode
	{
		if (taskNodes == null)
		{
			return;
		}
		foreach (Node<T> value in taskNodes.Values)
		{
			if (string.IsNullOrEmpty(value.ParentId))
			{
				continue;
			}
			if (taskNodes.ContainsKey(value.ParentId))
			{
				Node<T> node = taskNodes[value.ParentId];
				if (node.ProjectId != value.ProjectId)
				{
					value.Value.RemoveParent();
					UtilLog.Info("SetTaskParent empty " + value.NodeId + " Project Error");
					value.ParentId = "";
				}
				else if (value.NodeId != value.ParentId && !node.ExistParent(value.NodeId))
				{
					node.Children.Add(value);
					value.Parent = taskNodes[value.ParentId];
					if (node.ExistParent(value.NodeId))
					{
						node.Children.Remove(value);
						value.Parent = null;
					}
				}
				else
				{
					value.Value.RemoveParent();
					UtilLog.Info("SetTaskParent empty " + value.NodeId + " loop " + value.ParentId);
					value.ParentId = "";
				}
			}
			else if (checkParentExist)
			{
				TaskBaseViewModel taskById = TaskCache.GetTaskById(value.ParentId);
				if (taskById != null && taskById.ProjectId != value.ProjectId)
				{
					UtilLog.Info("SetTaskParent empty " + value.NodeId + " Project Error");
					value.Value.RemoveParent();
					value.ParentId = "";
				}
			}
		}
		if (buildByChildIds)
		{
			BuildNodeTreeByChildIds(taskNodes);
		}
	}

	private static void BuildNodeTreeByChildIds<T>(Dictionary<string, Node<T>> taskNodes) where T : INode
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		foreach (Node<T> value in taskNodes.Values)
		{
			if (!CanUseChildIds(value))
			{
				continue;
			}
			List<string> childrenIds = GetChildrenIds(value);
			if (childrenIds == null || childrenIds.Count == 0)
			{
				continue;
			}
			List<string> list = new List<string>();
			foreach (string item in childrenIds.Where((string id) => !string.IsNullOrEmpty(id)).Distinct())
			{
				if (taskNodes.ContainsKey(item) && CanUseChildIds(taskNodes[item]))
				{
					list.Add(item);
					dictionary2[item] = ((!dictionary2.ContainsKey(item)) ? 1 : (dictionary2[item] + 1));
				}
			}
			dictionary[value.NodeId] = list;
		}
		foreach (Node<T> value2 in taskNodes.Values)
		{
			if (!dictionary.ContainsKey(value2.NodeId))
			{
				continue;
			}
			foreach (string item2 in dictionary[value2.NodeId])
			{
				Node<T> child = taskNodes[item2];
				if (!child.HasParent && string.IsNullOrEmpty(child.ParentId) && !(value2.NodeId == child.NodeId) && !(value2.ProjectId != child.ProjectId) && !SyncStatusDao.HasSetParentSyncStatus(child.NodeId) && (!dictionary2.ContainsKey(item2) || dictionary2[item2] <= 1) && !value2.ExistParent(child.NodeId) && !value2.Children.Exists((Node<T> node) => node.NodeId == child.NodeId) && value2.GetLevel(0) < 4)
				{
					value2.Children.Add(child);
					child.Parent = value2;
					child.ParentId = value2.NodeId;
				}
			}
		}
	}

	private static bool CanUseChildIds<T>(Node<T> node) where T : INode
	{
		if (node.Value is TaskBaseViewModel taskBaseViewModel)
		{
			return taskBaseViewModel.IsTask;
		}
		if (node.Value is DisplayItemModel displayItemModel)
		{
			if (displayItemModel.IsTask)
			{
				return displayItemModel.Kind != "NOTE";
			}
			return false;
		}
		return true;
	}

	private static List<string> GetChildrenIds<T>(Node<T> node) where T : INode
	{
		string value = (node.Value as TaskBaseViewModel)?.ChildrenIds;
		if (node.Value is DisplayItemModel displayItemModel)
		{
			value = displayItemModel.SourceViewModel?.ChildrenIds;
		}
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		try
		{
			return JsonConvert.DeserializeObject<List<string>>(value);
		}
		catch (Exception ex)
		{
			UtilLog.Warn("BuildNodeTree childrenIds invalid " + node.NodeId + ", " + ex.Message);
			return null;
		}
	}

	private static void SortNodes<T>(List<Node<T>> nodes, Dictionary<string, int> dict) where T : INode
	{
		if (nodes == null || nodes.Count == 0)
		{
			return;
		}
		nodes.ForEach(delegate(Node<T> node)
		{
			SortNodes(node.Children, dict);
		});
		nodes.Sort(delegate(Node<T> a, Node<T> b)
		{
			if (dict.ContainsKey(a.NodeId) && dict.ContainsKey(b.NodeId))
			{
				return dict[a.NodeId].CompareTo(dict[b.NodeId]);
			}
			return dict.ContainsKey(b.NodeId) ? 1 : (-1);
		});
	}

	public static List<T> GetModelsFromNodes<T>(List<Node<T>> nodes) where T : INode
	{
		List<T> list = new List<T>();
		foreach (Node<T> node in nodes)
		{
			if (!node.HasParent)
			{
				list.Add(node.Value);
				list.AddRange(node.GetAllChildrenValue());
			}
		}
		return list;
	}

	public static List<Node<TaskBaseViewModel>> GetTaskNodeTree(List<TaskBaseViewModel> tasks)
	{
		Dictionary<string, Node<TaskBaseViewModel>> dictionary = ((IEnumerable<TaskBaseViewModel>)tasks).ToDictionaryEx((Func<TaskBaseViewModel, string>)((TaskBaseViewModel t) => t.Id), (Func<TaskBaseViewModel, Node<TaskBaseViewModel>>)((TaskBaseViewModel t) => new TaskNode(t)));
		BuildNodeTree(dictionary);
		return dictionary.Values.ToList();
	}
}
