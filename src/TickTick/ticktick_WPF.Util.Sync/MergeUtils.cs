using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Views.MarkDown;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util.Sync;

public static class MergeUtils
{
	public static void Merge(ProjectModel original, ProjectModel delta, ProjectModel revised)
	{
		revised.name = GetRevisedValue(original.name, delta.name, revised.name);
		revised.color = GetRevisedValue(original.color, delta.color, revised.color);
		revised.muted = GetRevisedValue(original.muted, delta.muted, revised.muted);
		revised.inAll = GetRevisedValue(original.inAll, delta.inAll, revised.inAll);
		revised.sortOrder = GetRevisedValue(original.sortOrder, delta.sortOrder, revised.sortOrder);
		revised.sortType = GetRevisedValue(original.sortType, delta.sortType, revised.sortType);
		revised.groupId = GetRevisedValue(original.groupId, delta.groupId, revised.groupId);
		revised.closed = GetRevisedValue(original.closed, delta.closed, revised.closed);
		revised.teamId = GetRevisedValue(original.teamId, delta.teamId, revised.teamId);
		revised.permission = GetRevisedValue(original.permission, delta.permission, revised.permission);
		revised.viewMode = GetRevisedValue(original.viewMode, delta.viewMode, revised.viewMode);
		revised.kind = GetRevisedValue(original.kind, delta.kind, revised.kind);
		bool flag = revised.sync_status == Constants.SyncStatus.SYNC_INIT.ToString();
		MergeTimeline(original, delta, revised, flag);
		if (delta.SortOption != null)
		{
			if (!flag && original.SortOption != null && revised.SortOption != null)
			{
				revised.SortOption.groupBy = GetRevisedValue(original.SortOption.groupBy, delta.SortOption.groupBy, revised.SortOption.groupBy);
				revised.SortOption.orderBy = GetRevisedValue(original.SortOption.orderBy, delta.SortOption.orderBy, revised.SortOption.orderBy);
				revised.SortOption.order = GetRevisedValue(original.SortOption.order, delta.SortOption.order, revised.SortOption.order);
			}
			else
			{
				revised.SortOption = delta.SortOption;
			}
		}
	}

	public static void Merge(ProjectGroupModel original, ProjectGroupModel delta, ProjectGroupModel revised)
	{
		revised.name = GetRevisedValue(original.name, delta.name, revised.name);
		revised.sortOrder = GetRevisedValue(original.sortOrder, delta.sortOrder, revised.sortOrder);
		revised.sortType = GetRevisedValue(original.sortType, delta.sortType, revised.sortType);
		revised.teamId = GetRevisedValue(original.teamId, delta.teamId, revised.teamId);
		revised.showAll = GetRevisedValue(original.showAll, delta.showAll, revised.showAll);
		revised.viewMode = GetRevisedValue(original.viewMode, delta.viewMode, revised.viewMode);
		bool flag = revised.sync_status == Constants.SyncStatus.SYNC_INIT.ToString();
		MergeTimeline(original, delta, revised, flag);
		if (delta.SortOption != null)
		{
			if (!flag && original.SortOption != null)
			{
				revised.SortOption.groupBy = GetRevisedValue(original.SortOption.groupBy, delta.SortOption.groupBy, revised.SortOption.groupBy);
				revised.SortOption.orderBy = GetRevisedValue(original.SortOption.orderBy, delta.SortOption.orderBy, revised.SortOption.orderBy);
				revised.SortOption.order = GetRevisedValue(original.SortOption.order, delta.SortOption.order, revised.SortOption.order);
			}
			else
			{
				revised.SortOption = delta.SortOption;
			}
		}
	}

	private static void MergeTimeline(ITimeline original, ITimeline delta, ITimeline revised, bool isInit)
	{
		if (revised.SyncTimeline != null)
		{
			revised.SyncTimeline.SortType = GetRevisedValue(original.SyncTimeline?.SortType, delta.SyncTimeline?.SortType, revised.SyncTimeline?.SortType);
			if (delta.SyncTimeline?.SortOption != null)
			{
				if (!isInit && original.SyncTimeline?.SortOption != null && revised.SyncTimeline.SortOption != null)
				{
					revised.SyncTimeline.SortOption.groupBy = GetRevisedValue(original.SyncTimeline.SortOption.groupBy, delta.SyncTimeline.SortOption.groupBy, revised.SyncTimeline.SortOption.groupBy);
					revised.SyncTimeline.SortOption.orderBy = GetRevisedValue(original.SyncTimeline.SortOption.orderBy, delta.SyncTimeline.SortOption.orderBy, revised.SyncTimeline.SortOption.orderBy);
					revised.SyncTimeline.SortOption.order = GetRevisedValue(original.SyncTimeline.SortOption.order, delta.SyncTimeline.SortOption.order, revised.SyncTimeline.SortOption.order);
				}
				else
				{
					revised.SyncTimeline.SortOption = delta.SyncTimeline.SortOption;
				}
			}
		}
		if (revised.Timeline != null)
		{
			revised.Timeline.SortType = revised.SyncTimeline?.SortType ?? revised.Timeline.SortType;
			revised.Timeline.sortOption = revised.SyncTimeline?.SortOption ?? revised.Timeline.sortOption;
		}
	}

	public static void Merge(FilterModel original, FilterModel delta, FilterModel revised)
	{
		revised.name = GetRevisedValue(original.name, delta.name, revised.name);
		revised.rule = GetRevisedValue(original.rule, delta.rule, revised.rule);
		revised.sortOrder = GetRevisedValue(original.sortOrder, delta.sortOrder, revised.sortOrder);
		revised.sortType = GetRevisedValue(original.sortType, delta.sortType, revised.sortType);
		revised.viewMode = GetRevisedValue(original.viewMode, delta.viewMode, revised.viewMode);
		bool flag = revised.syncStatus == 3;
		MergeTimeline(original, delta, revised, flag);
		if (delta.SortOption != null)
		{
			if (!flag && original.SortOption != null)
			{
				revised.SortOption.groupBy = GetRevisedValue(original.SortOption.groupBy, delta.SortOption.groupBy, revised.SortOption.groupBy);
				revised.SortOption.orderBy = GetRevisedValue(original.SortOption.orderBy, delta.SortOption.orderBy, revised.SortOption.orderBy);
				revised.SortOption.order = GetRevisedValue(original.SortOption.order, delta.SortOption.order, revised.SortOption.order);
			}
			else
			{
				revised.SortOption = delta.SortOption;
			}
		}
	}

	public static void Merge(TagModel original, TagModel delta, TagModel revised)
	{
		revised.sortOrder = GetRevisedValue(original.sortOrder, delta.sortOrder, revised.sortOrder);
		revised.sortType = GetRevisedValue(original.sortType, delta.sortType, revised.sortType);
		revised.color = GetRevisedValue(original.color, delta.color, revised.color);
		revised.label = GetRevisedValue(original.label, delta.label, revised.label);
		revised.parent = GetRevisedValue(original.parent, delta.parent, revised.parent);
		MergeTimeline(original, delta, revised, isInit: false);
		if (delta.SortOption != null)
		{
			if (revised.status != 3 && original.SortOption != null)
			{
				revised.SortOption.groupBy = GetRevisedValue(original.SortOption.groupBy, delta.SortOption.groupBy, revised.SortOption.groupBy);
				revised.SortOption.orderBy = GetRevisedValue(original.SortOption.orderBy, delta.SortOption.orderBy, revised.SortOption.orderBy);
				revised.SortOption.order = GetRevisedValue(original.SortOption.order, delta.SortOption.order, revised.SortOption.order);
			}
			else
			{
				revised.SortOption = delta.SortOption;
			}
		}
	}

	public static void Merge(TaskModel original, TaskModel delta, TaskModel revised)
	{
		string revisedDiffText = GetRevisedDiffText(original.title, delta.title, revised.title);
		Log("title", original.title, delta.title, revised.title, revisedDiffText);
		revised.title = revisedDiffText;
		int revisedValue = GetRevisedValue(original.priority, delta.priority, revised.priority, delta.modifiedTime > revised.modifiedTime);
		Log("priority", original.priority, delta.priority, revised.priority, revisedValue);
		revised.priority = revisedValue;
		int revisedValue2 = GetRevisedValue(original.status, delta.status, revised.status, delta.modifiedTime > revised.modifiedTime);
		Log("status", original.status, delta.status, revised.status, revisedValue2);
		revised.status = revisedValue2;
		long revisedValue3 = GetRevisedValue(original.sortOrder, delta.sortOrder, revised.sortOrder, delta.modifiedTime > revised.modifiedTime);
		Log("sortOrder", original.sortOrder, delta.sortOrder, revised.sortOrder, revisedValue3);
		revised.sortOrder = revisedValue3;
		int? revisedValue4 = GetRevisedValue(original.progress, delta.progress, revised.progress, delta.modifiedTime > revised.modifiedTime);
		Log("progress", original.progress, delta.progress, revised.progress, revisedValue4);
		revised.progress = revisedValue4;
		revised.startDate = GetRevisedValue(original.startDate, delta.startDate, revised.startDate, delta.modifiedTime > revised.modifiedTime);
		revised.dueDate = GetRevisedValue(original.dueDate, delta.dueDate, revised.dueDate, delta.modifiedTime > revised.modifiedTime);
		revised.isAllDay = GetRevisedValue(original.isAllDay, delta.isAllDay, revised.isAllDay, delta.modifiedTime > revised.modifiedTime);
		revised.timeZone = GetRevisedValue(original.timeZone, delta.timeZone, revised.timeZone, delta.modifiedTime > revised.modifiedTime);
		revised.isFloating = GetRevisedValue(original.isFloating, delta.isFloating, revised.isFloating, delta.modifiedTime > revised.modifiedTime);
		revised.annoyingAlert = GetRevisedValue(original.annoyingAlert, delta.annoyingAlert, revised.annoyingAlert, delta.modifiedTime > revised.modifiedTime);
		revised.notionBlock = delta.notionBlock;
		revised.parentId = GetRevisedValue(original.parentId, delta.parentId, revised.parentId);
		List<string> revisedTags = GetRevisedTags(original.tags?.ToList() ?? new List<string>(), delta.tags?.ToList() ?? new List<string>(), revised.tags?.ToList() ?? new List<string>());
		Log("tag", string.Join(",", original.tags?.ToList() ?? new List<string>()), string.Join(",", delta.tags?.ToList() ?? new List<string>()), string.Join(",", revised.tags?.ToList() ?? new List<string>()), string.Join(",", revisedTags));
		revised.tags = revisedTags.ToArray();
		revised.tag = TagSerializer.ToJsonContent(revisedTags);
		revised.exDate = GetRevisedValue(original.exDate, delta.exDate, revised.exDate, delta.modifiedTime > revised.modifiedTime);
		MergeChecklistItems(original, delta, revised);
		MergeReminders(original, delta, revised);
		MergePomoSummaries(original, delta, revised);
		revised.columnId = GetRevisedValue(original.columnId, delta.columnId, revised.columnId, delta.modifiedTime > revised.modifiedTime);
		revised.completedTime = GetRevisedValue(original.completedTime, delta.completedTime, revised.completedTime, delta.modifiedTime > revised.modifiedTime);
		revised.completedUserId = GetRevisedValue(original.completedUserId, delta.completedUserId, revised.completedUserId, delta.modifiedTime > revised.modifiedTime);
		revised.imgMode = GetRevisedValue(original.imgMode, delta.imgMode, revised.imgMode);
		revised.pinnedTime = GetRevisedValue(original.pinnedTime, delta.pinnedTime, revised.pinnedTime, delta.modifiedTime > revised.modifiedTime);
		revised.repeatFlag = GetRevisedValue(original.repeatFlag, delta.repeatFlag, revised.repeatFlag, delta.modifiedTime > revised.modifiedTime);
		revised.repeatFrom = GetRevisedValue(original.repeatFrom, delta.repeatFrom, revised.repeatFrom, delta.modifiedTime > revised.modifiedTime);
	}

	private static void MergePomoSummaries(TaskModel original, TaskModel delta, TaskModel revised)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		PomodoroSummaryModel pomodoroSummaryModel = original.FocusSummaries?.FirstOrDefault((PomodoroSummaryModel f) => f.userId == userId);
		PomodoroSummaryModel pomodoroSummaryModel2 = delta.FocusSummaries?.FirstOrDefault((PomodoroSummaryModel f) => f.userId == userId);
		PomodoroSummaryModel pomodoroSummaryModel3 = revised.FocusSummaries?.FirstOrDefault((PomodoroSummaryModel f) => f.userId == userId);
		if (pomodoroSummaryModel3 == null)
		{
			if (pomodoroSummaryModel2 != null)
			{
				List<PomodoroSummaryModel> list = revised.FocusSummaries?.ToList() ?? new List<PomodoroSummaryModel>();
				list.Add(pomodoroSummaryModel2);
				revised.FocusSummaries = list.ToArray();
			}
		}
		else if (pomodoroSummaryModel2 != null && pomodoroSummaryModel != null)
		{
			pomodoroSummaryModel3.count = GetRevisedValue(pomodoroSummaryModel.count, pomodoroSummaryModel2.count, pomodoroSummaryModel3.count);
			pomodoroSummaryModel3.PomoDuration = GetRevisedValue(pomodoroSummaryModel.PomoDuration, pomodoroSummaryModel2.PomoDuration, pomodoroSummaryModel3.PomoDuration);
			pomodoroSummaryModel3.StopwatchDuration = GetRevisedValue(pomodoroSummaryModel.StopwatchDuration, pomodoroSummaryModel2.StopwatchDuration, pomodoroSummaryModel3.StopwatchDuration);
			pomodoroSummaryModel3.estimatedPomo = GetRevisedValue(pomodoroSummaryModel.estimatedPomo, pomodoroSummaryModel2.estimatedPomo, pomodoroSummaryModel3.estimatedPomo);
			pomodoroSummaryModel3.EstimatedDuration = GetRevisedValue(pomodoroSummaryModel.EstimatedDuration, pomodoroSummaryModel2.EstimatedDuration, pomodoroSummaryModel3.EstimatedDuration);
			pomodoroSummaryModel3.focusesString = MergeFocuses(pomodoroSummaryModel.focusesString, pomodoroSummaryModel2.focusesString, pomodoroSummaryModel3.focusesString);
		}
	}

	private static string MergeFocuses(string original, string delta, string revised)
	{
		List<object[]> list = JsonConvert.DeserializeObject<List<object[]>>(delta) ?? new List<object[]>();
		List<object[]>? obj = JsonConvert.DeserializeObject<List<object[]>>(revised) ?? new List<object[]>();
		Dictionary<string, object[]> dictionary = GetDict(JsonConvert.DeserializeObject<List<object[]>>(original));
		Dictionary<string, object[]> dictionary2 = GetDict(list);
		Dictionary<string, object[]> dictionary3 = GetDict(obj);
		foreach (object[] item in obj)
		{
			if (item[0] is string key && dictionary.TryGetValue(key, out var value) && item[1] == value[1] && item[2] == value[2])
			{
				if (dictionary2.ContainsKey(key))
				{
					item[1] = dictionary2[key][1];
					item[2] = dictionary2[key][2];
				}
				else
				{
					dictionary3.Remove(key);
				}
			}
		}
		foreach (object[] item2 in list)
		{
			if (item2[0] is string key2 && !dictionary3.ContainsKey(key2))
			{
				dictionary3[key2] = item2;
			}
		}
		return JsonConvert.SerializeObject(dictionary3.Values.ToList());
		static Dictionary<string, object[]> GetDict(List<object[]> focuses)
		{
			Dictionary<string, object[]> dictionary4 = new Dictionary<string, object[]>();
			if (focuses == null)
			{
				return dictionary4;
			}
			foreach (object[] focuse in focuses)
			{
				if (focuse[0] is string key3)
				{
					dictionary4[key3] = focuse;
				}
			}
			return dictionary4;
		}
	}

	private static void MergeReminders(TaskModel original, TaskModel delta, TaskModel revised)
	{
		Dictionary<string, TaskReminderModel> dictionary = CreateItemsDict(original.reminders, (TaskReminderModel item) => item.id);
		Dictionary<string, TaskReminderModel> dictionary2 = CreateItemsDict(revised.reminders, (TaskReminderModel item) => item.id);
		Dictionary<string, TaskReminderModel> dictionary3 = new Dictionary<string, TaskReminderModel>();
		if (delta.reminders != null && delta.reminders.Length != 0)
		{
			TaskReminderModel[] reminders = delta.reminders;
			foreach (TaskReminderModel taskReminderModel in reminders)
			{
				if (!string.IsNullOrEmpty(taskReminderModel.id) && !dictionary3.ContainsKey(taskReminderModel.id))
				{
					dictionary3.Add(taskReminderModel.id, taskReminderModel);
					if (dictionary.ContainsKey(taskReminderModel.id) && dictionary2.ContainsKey(taskReminderModel.id))
					{
						TaskReminderModel taskReminderModel2 = dictionary[taskReminderModel.id];
						TaskReminderModel taskReminderModel3 = dictionary2[taskReminderModel.id];
						taskReminderModel3.trigger = GetRevisedValue(taskReminderModel2.trigger, taskReminderModel.trigger, taskReminderModel3.trigger);
					}
					if (dictionary.ContainsKey(taskReminderModel.id) && !dictionary2.ContainsKey(taskReminderModel.id) && dictionary[taskReminderModel.id].trigger != taskReminderModel.trigger)
					{
						dictionary2.Add(taskReminderModel.id, taskReminderModel);
					}
					if (!dictionary.ContainsKey(taskReminderModel.id) && !dictionary2.ContainsKey(taskReminderModel.id))
					{
						dictionary2.Add(taskReminderModel.id, taskReminderModel);
					}
				}
			}
		}
		foreach (TaskReminderModel value in dictionary.Values)
		{
			if (!dictionary3.ContainsKey(value.id) && dictionary2.ContainsKey(value.id))
			{
				TaskReminderModel taskReminderModel4 = dictionary2[value.id];
				if (value.trigger == taskReminderModel4.trigger)
				{
					dictionary2.Remove(value.id);
				}
			}
		}
		revised.reminders = FilterDuplicateReminders(dictionary2.Values.ToList()).ToArray();
	}

	private static List<TaskReminderModel> FilterDuplicateReminders(IEnumerable<TaskReminderModel> reminders)
	{
		List<TaskReminderModel> list = new List<TaskReminderModel>();
		List<string> list2 = new List<string>();
		foreach (TaskReminderModel reminder in reminders)
		{
			if (!list2.Contains(reminder.trigger))
			{
				list2.Add(reminder.trigger);
				list.Add(reminder);
			}
		}
		if (list.Count > 5)
		{
			return list.Take(5).ToList();
		}
		return list;
	}

	private static void MergeChecklistItems(TaskModel original, TaskModel delta, TaskModel revised)
	{
		string revisedValue = GetRevisedValue(original.kind, delta.kind, revised.kind, delta.modifiedTime > revised.modifiedTime);
		Log("kind", original.kind, delta.kind, revised.kind, revisedValue);
		if (delta.kind == revised.kind)
		{
			switch (revisedValue)
			{
			case "CHECKLIST":
			{
				string revisedDiffText = GetRevisedDiffText(original.desc, delta.desc, revised.desc);
				Log("desc", original.desc, delta.desc, revised.desc, revisedDiffText);
				revised.desc = revisedDiffText;
				revised.items = MergeChecklistItems(original.items, delta.items, revised.items);
				break;
			}
			case "TEXT":
			case "NOTE":
			{
				string text = GetRevisedDiffText(original.content, delta.content, revised.content) ?? string.Empty;
				Log("content", original.content, delta.content, revised.content, text);
				MatchCollection matchCollection = MarkDownEditor.AttachmentRegex.Matches(text);
				if (matchCollection.Count > 0 && matchCollection.Count > 0)
				{
					Dictionary<int, Match> dictionary = new Dictionary<int, Match>();
					HashSet<string> hashSet = new HashSet<string>();
					for (int i = 0; i < matchCollection.Count; i++)
					{
						Match match = matchCollection[i];
						if (!MarkDownEditor.IsInternalAttachmentMatch(text, match))
						{
							continue;
						}
						string[] array = match.Groups[2].Value.Split('/');
						if (array.Length == 2)
						{
							if (hashSet.Contains(array[0]))
							{
								dictionary[match.Index] = match;
							}
							else
							{
								hashSet.Add(array[0]);
							}
						}
					}
					foreach (int item in dictionary.Keys.OrderByDescending((int result) => result).ToList())
					{
						if (dictionary.TryGetValue(item, out var value))
						{
							text = text.Remove(item, Math.Min(text.Length - item, value.Length));
						}
					}
				}
				revised.content = text;
				revised.desc = string.Empty;
				revised.items = null;
				break;
			}
			}
		}
		else if (revisedValue == revised.kind)
		{
			switch (revisedValue)
			{
			case "NOTE":
			case "TEXT":
				revised.desc = string.Empty;
				revised.items = null;
				break;
			}
		}
		else
		{
			switch (revisedValue)
			{
			case "CHECKLIST":
				revised.desc = delta.desc;
				revised.items = delta.items;
				break;
			case "NOTE":
			case "TEXT":
				revised.content = delta.content;
				revised.desc = string.Empty;
				revised.items = null;
				break;
			}
		}
		revised.kind = revisedValue;
	}

	private static TaskDetailItemModel[] MergeChecklistItems(TaskDetailItemModel[] original, TaskDetailItemModel[] delta, TaskDetailItemModel[] revised)
	{
		Dictionary<string, TaskDetailItemModel> dictionary = CreateItemsDict(original, (TaskDetailItemModel item) => item.id);
		Dictionary<string, TaskDetailItemModel> dictionary2 = CreateItemsDict(revised, (TaskDetailItemModel item) => item.id);
		Dictionary<string, TaskDetailItemModel> dictionary3 = new Dictionary<string, TaskDetailItemModel>();
		if (delta != null && delta.Length != 0)
		{
			foreach (TaskDetailItemModel taskDetailItemModel in delta)
			{
				string id = taskDetailItemModel.id;
				if (string.IsNullOrEmpty(id))
				{
					continue;
				}
				dictionary3.Add(id, taskDetailItemModel);
				if (dictionary.ContainsKey(id) && dictionary2.ContainsKey(id))
				{
					TaskDetailItemModel taskDetailItemModel2 = dictionary[id];
					TaskDetailItemModel taskDetailItemModel3 = dictionary2[id];
					string revisedDiffText = GetRevisedDiffText(taskDetailItemModel2.title, taskDetailItemModel.title, taskDetailItemModel3.title);
					Log("item_title", taskDetailItemModel2.title, taskDetailItemModel.title, taskDetailItemModel3.title, revisedDiffText);
					taskDetailItemModel3.title = revisedDiffText;
					taskDetailItemModel3.sortOrder = GetRevisedValue(taskDetailItemModel2.sortOrder, taskDetailItemModel.sortOrder, taskDetailItemModel3.sortOrder);
					taskDetailItemModel3.status = GetRevisedValue(taskDetailItemModel2.status, taskDetailItemModel.status, taskDetailItemModel3.status);
					taskDetailItemModel3.completedTime = GetRevisedValue(taskDetailItemModel2.completedTime, taskDetailItemModel.completedTime, taskDetailItemModel3.completedTime);
					taskDetailItemModel3.snoozeReminderTime = GetRevisedValue(taskDetailItemModel2.snoozeReminderTime, taskDetailItemModel.snoozeReminderTime, taskDetailItemModel3.snoozeReminderTime);
				}
				else if (dictionary.ContainsKey(id))
				{
					if (DiffCheckItemExcludeSortOrder(dictionary[id], taskDetailItemModel) || revised == null || revised.Length == 0)
					{
						dictionary2.Add(id, taskDetailItemModel);
					}
				}
				else
				{
					dictionary2.Add(id, taskDetailItemModel);
				}
			}
		}
		foreach (TaskDetailItemModel value in dictionary.Values)
		{
			if (!dictionary3.ContainsKey(value.id) && dictionary2.ContainsKey(value.id))
			{
				TaskDetailItemModel taskDetailItemModel4 = dictionary2[value.id];
				if (!DiffCheckItemExcludeSortOrder(value, taskDetailItemModel4))
				{
					dictionary2.Remove(taskDetailItemModel4.id);
				}
			}
		}
		return dictionary2.Values.ToArray();
	}

	private static bool DiffCheckItemExcludeSortOrder(TaskDetailItemModel item1, TaskDetailItemModel item2)
	{
		if (item1.title != item2.title)
		{
			return true;
		}
		if (item1.status != item2.status)
		{
			return true;
		}
		if (item1.isAllDay != item2.isAllDay)
		{
			return true;
		}
		if (item1.startDate != item2.startDate)
		{
			return true;
		}
		return false;
	}

	private static Dictionary<string, T> CreateItemsDict<T>(T[] models, Func<T, string> getId)
	{
		List<T> obj = models?.ToList() ?? new List<T>();
		Dictionary<string, T> dictionary = new Dictionary<string, T>();
		foreach (T item in obj)
		{
			if (!string.IsNullOrEmpty(getId(item)) && !dictionary.ContainsKey(getId(item)))
			{
				dictionary.Add(getId(item), item);
			}
		}
		return dictionary;
	}

	private static List<string> GetRevisedTags(IEnumerable<string> original, ICollection<string> delta, ICollection<string> revised)
	{
		List<string> list = revised.ToList();
		list.AddRange(delta.Except(revised));
		foreach (string item in original)
		{
			if (!delta.Contains(item) || !revised.Contains(item))
			{
				list.Remove(item);
			}
		}
		return list;
	}

	public static string GetRevisedDiffText(string original, string delta, string revised)
	{
		if (string.Equals(original, revised))
		{
			return delta;
		}
		if (string.Equals(original, delta))
		{
			return revised;
		}
		diff_match_patch diff_match_patch = new diff_match_patch();
		return diff_match_patch.patch_apply(diff_match_patch.patch_make(original ?? string.Empty, delta ?? string.Empty), revised ?? string.Empty)[0].ToString();
	}

	private static T GetRevisedValue<T>(T original, T delta, T revised, bool deltaFirst = false)
	{
		string text = string.Empty;
		if (original != null)
		{
			text = original.ToString();
		}
		string value = string.Empty;
		if (delta != null)
		{
			value = delta.ToString();
		}
		string value2 = string.Empty;
		if (revised != null)
		{
			value2 = revised.ToString();
		}
		if (text.Equals(value2))
		{
			return delta;
		}
		if (text.Equals(value))
		{
			return revised;
		}
		if (!deltaFirst)
		{
			return revised;
		}
		return delta;
	}

	private static void Log(string key, object original, object delta, object revised, object merged)
	{
		if (App.IsAdmin)
		{
			UtilLog.Info("\n******************** " + key + " ******************** \n" + $"original (backup):{original}\n" + $"delta (server)   :{delta}\n" + $"revised (local)  :{revised}\n" + $"merge (final)    :{merged}\n");
		}
	}

	public static void MergeTaskTags(TaskModel original, TaskModel delta, TaskModel revised)
	{
		List<string> revised2 = revised.tags?.ToList() ?? TagSerializer.ToTags(revised.tag);
		List<string> revisedTags = GetRevisedTags(original.tags?.ToList() ?? new List<string>(), delta.tags?.ToList() ?? new List<string>(), revised2);
		revised.tags = revisedTags.ToArray();
		revised.tag = TagSerializer.ToJsonContent(revisedTags);
	}
}
