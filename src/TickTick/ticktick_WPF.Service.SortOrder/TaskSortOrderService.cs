using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Service.SortOrder;

public class TaskSortOrderService
{
	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass4_0
	{
		public int updateCount;

		public Action<Task<int>> _003C_003E9__1;

		internal void _003CFetchAllAsync_003Eb__1(Task<int> result)
		{
			if (result.IsCompleted)
			{
				updateCount += result.Result;
			}
		}

		internal bool _003CFetchAllAsync_003Eb__0(Task t)
		{
			return updateCount > 0;
		}
	}

	private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>> CachedOrders = new ConcurrentDictionary<string, ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>>();

	public static async Task<List<SyncSortOrderModel>> GetAsyncList(string sortOrderType, string groupId, bool force = false)
	{
		return (await GetAsync(sortOrderType, groupId, force))?.ToList();
	}

	private static async Task<BlockingList<SyncSortOrderModel>> GetAsync(string sortOrderType, string groupId, bool force = false)
	{
		if (string.IsNullOrEmpty(groupId))
		{
			return null;
		}
		if (string.IsNullOrEmpty(sortOrderType))
		{
			return null;
		}
		if (force)
		{
			await FetchAsync(sortOrderType, groupId);
		}
		if (CachedOrders.TryGetValue(sortOrderType, out var value) && value.TryGetValue(groupId, out var value2))
		{
			return value2;
		}
		if (!force)
		{
			await FetchAsync(sortOrderType, groupId);
			if (CachedOrders.TryGetValue(sortOrderType, out var value3) && value3.TryGetValue(groupId, out var value4))
			{
				return value4;
			}
		}
		BlockingList<SyncSortOrderModel> blockingList = new BlockingList<SyncSortOrderModel>();
		if (!CachedOrders.ContainsKey(sortOrderType))
		{
			ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>> concurrentDictionary = new ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>();
			concurrentDictionary.TryAdd(groupId, blockingList);
			CachedOrders.TryAdd(sortOrderType, concurrentDictionary);
		}
		else
		{
			CachedOrders[sortOrderType].AddOrUpdate(groupId, blockingList, (string key, BlockingList<SyncSortOrderModel> oldValue) => oldValue);
		}
		return blockingList;
	}

	public static async Task<int> FetchAsync(string sortOrderType, string groupId = null)
	{
		int updateCount = 0;
		List<SyncSortOrderModel> list = await SyncSortOrderDao.GetAllOrderBySortOrderType(sortOrderType, groupId);
		ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>> value5;
		if (list.Any())
		{
			Dictionary<string, Dictionary<string, SyncSortOrderModel>> dictionary = new Dictionary<string, Dictionary<string, SyncSortOrderModel>>();
			foreach (SyncSortOrderModel item in list)
			{
				if (!dictionary.ContainsKey(item.GroupId))
				{
					dictionary[item.GroupId] = new Dictionary<string, SyncSortOrderModel>();
				}
				Dictionary<string, SyncSortOrderModel> dictionary2 = dictionary[item.GroupId];
				if (!dictionary2.ContainsKey(item.EntityId))
				{
					dictionary2[item.EntityId] = item;
				}
			}
			if (!CachedOrders.TryGetValue(sortOrderType, out var value))
			{
				value = new ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>();
			}
			foreach (KeyValuePair<string, Dictionary<string, SyncSortOrderModel>> item2 in dictionary)
			{
				string key = item2.Key;
				List<SyncSortOrderModel> models = item2.Value.Values.ToList();
				if (value.TryGetValue(key, out var value2))
				{
					value2.Clear();
					value2.AddRange(models);
				}
				else
				{
					BlockingList<SyncSortOrderModel> value3 = new BlockingList<SyncSortOrderModel>(models);
					value.TryAdd(key, value3);
				}
				CachedOrders.TryAdd(sortOrderType, value);
				updateCount++;
			}
		}
		else if (string.IsNullOrEmpty(groupId))
		{
			CachedOrders.TryRemove(sortOrderType, out var _);
		}
		else if (CachedOrders.TryGetValue(sortOrderType, out value5))
		{
			value5.TryRemove(groupId, out var _);
		}
		return updateCount;
	}

	public static async Task FetchAllAsync()
	{
		new _003C_003Ec__DisplayClass4_0();
		CachedOrders.Clear();
	}

	public static async Task DeleteAllSortOrderBySortOptionInListId(SortOption option, string id)
	{
		string sortKey = option.GetSortKey();
		if (string.IsNullOrEmpty(sortKey))
		{
			return;
		}
		int num = sortKey.IndexOf("{0}");
		if (num == -1)
		{
			return;
		}
		string value = sortKey.Substring(0, num);
		string value2 = sortKey.Substring(num + "{0}".Length);
		foreach (string item in CachedOrders.Keys.ToList())
		{
			if (!item.StartsWith(value) || !item.EndsWith(value2) || !CachedOrders.TryGetValue(item, out var value3))
			{
				continue;
			}
			foreach (string key in value3.Keys)
			{
				if (key == id)
				{
					value3[key]?.Clear();
				}
			}
		}
		if (await SyncSortOrderDao.SetAllDeletedByGroupId(option, id) > 0)
		{
			SyncManager.TryDelaySync();
		}
	}

	public static async Task DeleteAllTaskSortOrdersInLists(IEnumerable<string> ids)
	{
		int num = 0;
		foreach (string id in ids.Distinct())
		{
			int num2 = num;
			num = num2 + await SyncSortOrderDao.SetAllTaskOrdersDeletedByGroupId(id);
			foreach (KeyValuePair<string, ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>> item in CachedOrders.Where((KeyValuePair<string, ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>> pair) => pair.Key == "taskPinned" || pair.Key.StartsWith("taskBy#")))
			{
				item.Value.TryRemove(id, out var _);
			}
		}
		if (num > 0)
		{
			SyncManager.TryDelaySync();
		}
	}

	public static async Task<SyncSortOrderModel> InsertOrUpdateAsync(string sortOrderType, string groupId, string entityId, int type = 1, long? sortOrder = null)
	{
		BlockingList<SyncSortOrderModel> blockingList = await GetAsync(sortOrderType, groupId);
		if (blockingList == null)
		{
			return null;
		}
		List<long> source = blockingList.SelectList((SyncSortOrderModel m) => m.SortOrder);
		SyncSortOrderModel orderModel = blockingList.FirstOrDefault((SyncSortOrderModel item) => item.EntityId == entityId);
		if (orderModel == null)
		{
			orderModel = new SyncSortOrderModel(sortOrderType)
			{
				EntityId = entityId,
				SortOrder = (sortOrder ?? (source.Any() ? (source.Min() - 268435456) : 0)),
				GroupId = groupId,
				Type = type,
				SyncStatus = 1
			};
			blockingList.Add(orderModel);
		}
		else
		{
			orderModel.SortOrder = sortOrder ?? (source.Any() ? (source.Min() - 268435456) : 0);
			orderModel.SyncStatus = 1;
			orderModel.Type = type;
		}
		await SyncSortOrderDao.InsertOrUpdate(orderModel);
		SyncManager.TryDelaySync();
		return orderModel;
	}

	public static async Task<SyncSortOrderModel> InsertAfterAsync(string sortOrderType, string groupId, string entityId, int type = 1, string columnId = "", string targetId = "")
	{
		if (string.IsNullOrWhiteSpace(columnId))
		{
			columnId = "";
		}
		List<SyncSortOrderModel> list = (await GetAsync(sortOrderType, groupId))?.ToList();
		if (list == null)
		{
			return null;
		}
		list.Sort((SyncSortOrderModel a, SyncSortOrderModel b) => a.SortOrder.CompareTo(b.SortOrder));
		SyncSortOrderModel syncSortOrderModel = list.FirstOrDefault((SyncSortOrderModel item) => item.EntityId == targetId);
		long sortOrder;
		if (syncSortOrderModel != null)
		{
			int num = list.IndexOf(syncSortOrderModel);
			sortOrder = ((num < list.Count - 1) ? (syncSortOrderModel.SortOrder / 2 + list[num + 1].SortOrder / 2) : (syncSortOrderModel.SortOrder + 268435456));
		}
		else
		{
			sortOrder = (list.Any() ? (list.Min((SyncSortOrderModel o) => o.SortOrder) - 268435456) : 0);
		}
		SyncSortOrderModel pinModel = list.FirstOrDefault((SyncSortOrderModel item) => item.EntityId == entityId);
		if (pinModel == null)
		{
			pinModel = new SyncSortOrderModel(sortOrderType)
			{
				EntityId = entityId,
				GroupId = groupId,
				Type = type,
				SyncStatus = 1,
				SortOrder = sortOrder
			};
			list.Add(pinModel);
		}
		else
		{
			pinModel.SyncStatus = 1;
			pinModel.SortOrder = sortOrder;
		}
		CachedOrders[sortOrderType][groupId] = new BlockingList<SyncSortOrderModel>(list);
		await SyncSortOrderDao.InsertOrUpdate(pinModel);
		SyncManager.TryDelaySync();
		return pinModel;
	}

	public static async Task SaveInits(Dictionary<string, Dictionary<string, Dictionary<string, SyncSortOrderModel>>> dict)
	{
		if (dict == null)
		{
			return;
		}
		foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, SyncSortOrderModel>>> item in dict)
		{
			string sortOrderType = item.Key;
			Dictionary<string, Dictionary<string, SyncSortOrderModel>> value = item.Value;
			foreach (KeyValuePair<string, Dictionary<string, SyncSortOrderModel>> item2 in value)
			{
				string key = item2.Key;
				List<SyncSortOrderModel> models = item2.Value.Values.ToList();
				List<SyncSortOrderModel> list = (await GetAsync(sortOrderType, key))?.ToList();
				if (list != null && list.Count > 0)
				{
					HashSet<string> existEntities = new HashSet<string>(list.Select((SyncSortOrderModel e) => e.EntityId));
					models.RemoveAll((SyncSortOrderModel m) => existEntities.Contains(m.EntityId));
				}
				await SyncSortOrderDao.InsertAllAsync(models);
			}
		}
	}

	public static async Task DeleteAsync(string sortOrderType, string groupId, string entityId, int type = 1)
	{
		if (string.IsNullOrEmpty(sortOrderType) || string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(entityId))
		{
			return;
		}
		SyncSortOrderModel syncSortOrderModel = await SyncSortOrderDao.GetSortOrderAsync(sortOrderType, groupId, entityId, type);
		if (syncSortOrderModel != null)
		{
			await SyncSortOrderDao.DeleteAsync(syncSortOrderModel);
		}
		if (CachedOrders.TryGetValue(sortOrderType, out var value) && value.TryGetValue(groupId, out var value2))
		{
			value2.RemoveAll((SyncSortOrderModel item) => item.EntityId == entityId && item.Type == type);
		}
	}

	public static async Task DeleteAllAsync(string sortOrderType, string groupId, List<string> ids, string columnId = "")
	{
		if (ids == null || string.IsNullOrEmpty(sortOrderType) || string.IsNullOrEmpty(groupId))
		{
			return;
		}
		HashSet<string> idSet = new HashSet<string>(ids);
		BlockingList<SyncSortOrderModel> list = await GetAsync(sortOrderType, groupId);
		if (list == null)
		{
			return;
		}
		List<SyncSortOrderModel> list2 = list.Where((SyncSortOrderModel item) => !idSet.Contains(item.EntityId) && item.ColumnId == columnId);
		foreach (SyncSortOrderModel item in list2)
		{
			await SyncSortOrderDao.DeleteAsync(item);
		}
		list.RemoveAll((SyncSortOrderModel item) => !idSet.Contains(item.EntityId) && item.ColumnId == columnId);
		SyncManager.TryDelaySync();
	}

	public static async Task<List<SyncSortOrderModel>> BatchResetSortOrder(string sortOrderType, string catId, List<(string, int)> ids, string columnId = "")
	{
		List<SyncSortOrderModel> result = new List<SyncSortOrderModel>();
		if (ids == null)
		{
			return result;
		}
		await DeleteAllAsync(sortOrderType, catId, ids.Select(((string, int) tuple2) => tuple2.Item1).ToList());
		BlockingList<SyncSortOrderModel> items = await GetAsync(sortOrderType, catId);
		int count = ids.Count;
		for (int i = 0; i < count; i++)
		{
			(string, int) tuple = ids[i];
			string id = tuple.Item1;
			int item = tuple.Item2;
			SyncSortOrderModel item2 = items?.FirstOrDefault((SyncSortOrderModel m) => m.EntityId == id);
			long num = 268435456L * (long)i;
			if (item2 != null)
			{
				item2.SortOrder = num;
				await InsertOrUpdateAsync(sortOrderType, catId, id, item, num);
			}
			else
			{
				item2 = await InsertOrUpdateAsync(sortOrderType, catId, id, item, num);
			}
			result.Add(item2);
		}
		if (result.Any())
		{
			SyncManager.TryDelaySync();
		}
		return result;
	}

	public static async Task NewTaskAdded(string taskId, string catId, SortOption sortOption, string frontId = null)
	{
		if (string.IsNullOrEmpty(taskId) || sortOption == null)
		{
			return;
		}
		if (catId.Contains("_special_id_"))
		{
			UtilLog.Info("NewTaskAdded in _special_id_");
			catId = "all";
		}
		string key = sortOption.GetSortKey();
		List<SyncSortOrderModel> list = await GetAsyncList(key, catId);
		if (list == null || list.Count == 0)
		{
			return;
		}
		long num = long.MaxValue;
		if (!string.IsNullOrEmpty(frontId))
		{
			SyncSortOrderModel syncSortOrderModel = list.FirstOrDefault((SyncSortOrderModel m) => m.EntityId == frontId);
			if (syncSortOrderModel != null)
			{
				list.Sort((SyncSortOrderModel a, SyncSortOrderModel b) => a.SortOrder.CompareTo(b.SortOrder));
				int num2 = list.IndexOf(syncSortOrderModel);
				if (num2 >= list.Count - 1)
				{
					num = syncSortOrderModel.SortOrder + 268435456;
				}
				else
				{
					SyncSortOrderModel syncSortOrderModel2 = list[num2 + 1];
					num = syncSortOrderModel.SortOrder + (syncSortOrderModel2.SortOrder - syncSortOrderModel.SortOrder) / 2;
				}
				UtilLog.Info(string.Format("NewTaskAdded in {0},id {1},sort {2},front {3}", "_special_id_", taskId, num, syncSortOrderModel.SortOrder));
			}
		}
		if (num == long.MaxValue)
		{
			if (TaskDefaultDao.GetDefaultSafely().AddTo == 0)
			{
				long val = list.Select((SyncSortOrderModel o) => o.SortOrder).Min();
				val = Math.Min(0L, val);
				num = val - 268435456;
			}
			else
			{
				long val2 = list.Select((SyncSortOrderModel o) => o.SortOrder).Max();
				val2 = Math.Max(0L, val2);
				num = val2 + 268435456;
			}
		}
		await InsertOrUpdateAsync(key, catId, taskId, 1, num);
	}

	public static async Task BatchResetOrders(List<DisplayItemModel> models, string sortKey, string cId)
	{
		if (models == null || models.Count == 0)
		{
			return;
		}
		long max = long.MinValue;
		long min = long.MaxValue;
		foreach (DisplayItemModel model in models)
		{
			if (model.SpecialOrder != long.MaxValue && model.SpecialOrder != long.MinValue)
			{
				if (model.SpecialOrder >= max)
				{
					max = model.SpecialOrder;
				}
				if (model.SpecialOrder <= min)
				{
					min = model.SpecialOrder;
				}
			}
		}
		if (max == long.MinValue)
		{
			max = 1 + 268435456L * (long)(models.Count - 1);
			min = 1L;
		}
		if (Math.Abs(max - min) < models.Count * 256)
		{
			max = Math.Max(9223372036586340351L, min + 268435456);
			min = max - 268435456;
		}
		List<SyncSortOrderModel> sortOrders = (await GetAsyncList(sortKey, cId, force: true)) ?? new List<SyncSortOrderModel>();
		long sortOrder = min;
		long step = ((models.Count <= 1) ? 0 : ((max - min) / (models.Count - 1)));
		foreach (DisplayItemModel m in models)
		{
			SyncSortOrderModel sortModel = sortOrders?.FirstOrDefault((SyncSortOrderModel s) => s.EntityId == m.EntityId);
			int entityTypeNum = EntityType.GetEntityTypeNum(m.Type);
			if (sortModel == null)
			{
				sortModel = new SyncSortOrderModel(sortKey)
				{
					EntityId = m.EntityId,
					SortOrder = sortOrder,
					GroupId = cId,
					Type = entityTypeNum,
					SyncStatus = 1
				};
				await SyncSortOrderDao.InsertAsync(sortModel);
				sortOrders.Add(sortModel);
			}
			else
			{
				sortModel.SortOrder = sortOrder;
				sortModel.SyncStatus = 1;
				sortModel.Type = entityTypeNum;
				await SyncSortOrderDao.UpdateAsync(sortModel);
			}
			m.SpecialOrder = sortOrder;
			sortOrder += step;
		}
		SetSortModels(sortKey, cId, sortOrders);
	}

	public static void SetSortModels(string sortKey, string cId, List<SyncSortOrderModel> result)
	{
		if (!string.IsNullOrEmpty(sortKey) && !string.IsNullOrEmpty(cId) && result != null)
		{
			if (!CachedOrders.ContainsKey(sortKey))
			{
				ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>> concurrentDictionary = new ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>();
				concurrentDictionary.TryAdd(cId, new BlockingList<SyncSortOrderModel>(result));
				CachedOrders.TryAdd(sortKey, concurrentDictionary);
			}
			else
			{
				CachedOrders[sortKey][cId] = new BlockingList<SyncSortOrderModel>(result);
			}
		}
	}

	public static void AddSortModels(string sortKey, string cId, List<SyncSortOrderModel> result)
	{
		if (string.IsNullOrEmpty(sortKey) || string.IsNullOrEmpty(cId) || result == null)
		{
			return;
		}
		if (!CachedOrders.ContainsKey(sortKey))
		{
			ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>> concurrentDictionary = new ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>>();
			concurrentDictionary.TryAdd(cId, new BlockingList<SyncSortOrderModel>(result));
			CachedOrders.TryAdd(sortKey, concurrentDictionary);
			return;
		}
		ConcurrentDictionary<string, BlockingList<SyncSortOrderModel>> concurrentDictionary2 = CachedOrders[sortKey];
		if (concurrentDictionary2.ContainsKey(cId) && concurrentDictionary2[cId] != null)
		{
			concurrentDictionary2[cId].AddRange(result);
		}
		else
		{
			concurrentDictionary2[cId] = new BlockingList<SyncSortOrderModel>(result);
		}
	}

	public static async Task OnColumnInit(string projectId, string defaultColumnId)
	{
		List<SyncSortOrderModel> list = await SyncSortOrderDao.GetNoColumnOrderInProject(projectId);
		if (list.Count == 0)
		{
			return;
		}
		List<SyncSortOrderModel> newOrders = new List<SyncSortOrderModel>();
		foreach (SyncSortOrderModel item in list)
		{
			SyncSortOrderModel syncSortOrderModel = new SyncSortOrderModel();
			SyncSortOrderModel.Copy(syncSortOrderModel, item);
			syncSortOrderModel.SyncStatus = 1;
			syncSortOrderModel.SortOrderType = item.SortOrderType.Replace("none", defaultColumnId);
			newOrders.Add(syncSortOrderModel);
			item.SyncStatus = 1;
			item.Deleted = 1;
		}
		await SyncSortOrderDao.UpdateAllAsync(list);
		await SyncSortOrderDao.InsertAllAsync(newOrders);
	}
}
