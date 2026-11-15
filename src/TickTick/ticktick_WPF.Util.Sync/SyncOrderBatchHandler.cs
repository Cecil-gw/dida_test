using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class SyncOrderBatchHandler : BatchHandler
{
	public SyncOrderBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public async Task MergeWithServer(SyncOrderBean syncOrderBean)
	{
		if (syncOrderBean != null && await MergeSyncOrder(syncOrderBean.OrderByType))
		{
			syncResult.TaskSortChanged = true;
		}
	}

	private async Task<bool> MergeSyncOrder(Dictionary<string, Dictionary<string, SortOrderData>> syncOrderBean)
	{
		if (syncOrderBean == null || !syncOrderBean.Any())
		{
			return false;
		}
		string log = "MergeSyncOrder:" + syncOrderBean.Count + " ,  ";
		SyncDataBean<SyncSortOrderModel> pullDataBean = new SyncDataBean<SyncSortOrderModel>();
		HashSet<(string SortOrderType, string GroupId, string EntityId, int Type)> orderKeys = new HashSet<(string, string, string, int)>();
		foreach (KeyValuePair<string, Dictionary<string, SortOrderData>> sortOrderType in syncOrderBean)
		{
			foreach (KeyValuePair<string, SortOrderData> group in sortOrderType.Value)
			{
				group.Value.Changed?.ForEach(delegate(SyncSortOrder order)
				{
					orderKeys.Add((sortOrderType.Key, group.Key, order.Id, order.Type));
				});
				group.Value.Deleted?.ForEach(delegate(SyncSortOrder order)
				{
					orderKeys.Add((sortOrderType.Key, group.Key, order.Id, order.Type));
				});
			}
		}
		List<SyncSortOrderModel> obj = await SyncSortOrderDao.GetLocalSortOrdersByKeys(orderKeys.ToList());
		Dictionary<(string, string, string, int), SyncSortOrderModel> dictionary = new Dictionary<(string, string, string, int), SyncSortOrderModel>();
		foreach (SyncSortOrderModel item in obj)
		{
			(string, string, string, int) key = (item.SortOrderType, item.GroupId, item.EntityId, item.Type);
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, item);
			}
		}
		HashSet<(string, string, string, int)> hashSet = new HashSet<(string, string, string, int)>();
		HashSet<(string, string, string, int)> hashSet2 = new HashSet<(string, string, string, int)>();
		HashSet<(string, string, string, int)> hashSet3 = new HashSet<(string, string, string, int)>();
		foreach (string key2 in syncOrderBean.Keys)
		{
			log = log + key2 + ": (";
			foreach (KeyValuePair<string, SortOrderData> item2 in syncOrderBean[key2])
			{
				SortOrderData value = item2.Value;
				if (value.Changed != null)
				{
					foreach (SyncSortOrder item3 in value.Changed)
					{
						(string, string, string, int) tuple = (key2, item2.Key, item3.Id, item3.Type);
						dictionary.TryGetValue(tuple, out var value2);
						if (value2 != null && value2.SyncStatus != 2 && value2.SyncStatus != 3)
						{
							continue;
						}
						SyncSortOrderModel syncSortOrderModel = new SyncSortOrderModel
						{
							UserId = userId,
							SortOrderType = key2,
							GroupId = item2.Key,
							EntityId = item3.Id,
							SortOrder = item3.Order.GetValueOrDefault(),
							SyncStatus = 2,
							Type = item3.Type
						};
						if (value2 == null)
						{
							pullDataBean.Addeds.Add(syncSortOrderModel);
							dictionary[tuple] = syncSortOrderModel;
							hashSet.Add(tuple);
						}
						else if (hashSet.Contains(tuple))
						{
							value2.SortOrder = syncSortOrderModel.SortOrder;
						}
						else if (syncSortOrderModel.SortOrder != value2.SortOrder)
						{
							value2.SortOrder = syncSortOrderModel.SortOrder;
							value2.SyncStatus = 2;
							if (hashSet2.Add(tuple))
							{
								pullDataBean.Updateds.Add(value2);
							}
						}
					}
				}
				if (value.Deleted != null)
				{
					foreach (SyncSortOrder item4 in value.Deleted)
					{
						(string, string, string, int) tuple2 = (key2, item2.Key, item4.Id, item4.Type);
						dictionary.TryGetValue(tuple2, out var value3);
						if (value3 == null || (value3.SyncStatus != 2 && value3.SyncStatus != 3))
						{
							continue;
						}
						if (hashSet.Remove(tuple2))
						{
							pullDataBean.Addeds.Remove(value3);
							dictionary.Remove(tuple2);
						}
						else if (hashSet3.Add(tuple2))
						{
							if (hashSet2.Remove(tuple2))
							{
								pullDataBean.Updateds.Remove(value3);
							}
							pullDataBean.Deleteds.Add(value3);
							dictionary.Remove(tuple2);
						}
					}
				}
				log = log + item2.Key + $":deleted {value.Deleted?.Count}, changed {value.Changed?.Count}";
			}
			log += ")  ";
		}
		await SyncSortOrderDao.SaveRemoteChangesToDb(pullDataBean);
		log += $"a: {pullDataBean.Addeds.Count} , u: {pullDataBean.Updateds.Count}, d: {pullDataBean.Deleteds.Count}";
		UtilLog.Info(log);
		return !pullDataBean.IsEmpty();
	}

	public static async Task<SyncOrderBean> CreateCommitBean()
	{
		SyncOrderBean syncOrderBean = new SyncOrderBean();
		SyncOrderBean syncOrderBean2 = syncOrderBean;
		syncOrderBean2.OrderByType = await CreateCommitOrderMap();
		return syncOrderBean;
	}

	public static async Task HandleCommitResult(BatchSyncOrderUpdateResult result)
	{
		await HandleCommitOrderResult(result.orderByType);
	}

	private static async Task HandleCommitOrderResult(BatchUpdateResult result)
	{
		SyncDataBean<SyncSortOrderModel> dateBean = new SyncDataBean<SyncSortOrderModel>();
		List<SyncSortOrderModel> list = await SyncSortOrderDao.GetNeedPostSortOrders();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (SyncSortOrderModel item in list)
		{
			if (result.Id2etag.Keys.Contains(item.SortOrderType))
			{
				if (item.SyncStatus != 2)
				{
					dateBean.Updateds.Add(item);
				}
				else
				{
					dateBean.Deleteds.Add(item);
				}
			}
		}
		await SyncSortOrderDao.SavePostResult(dateBean);
	}

	private static async Task<Dictionary<string, Dictionary<string, SortOrderData>>> CreateCommitOrderMap()
	{
		Dictionary<string, Dictionary<string, SortOrderData>> commitBeanMap = new Dictionary<string, Dictionary<string, SortOrderData>>();
		Dictionary<string, Dictionary<string, List<SyncSortOrderModel>>> dictionary = await SyncSortOrderDao.GetNeedPostMap();
		if (dictionary == null || dictionary.Count == 0)
		{
			return commitBeanMap;
		}
		foreach (string key in dictionary.Keys)
		{
			Dictionary<string, List<SyncSortOrderModel>> dictionary2 = dictionary[key];
			Dictionary<string, SortOrderData> dictionary3 = new Dictionary<string, SortOrderData>();
			foreach (string key2 in dictionary2.Keys)
			{
				List<SyncSortOrder> list = new List<SyncSortOrder>();
				List<SyncSortOrder> list2 = new List<SyncSortOrder>();
				foreach (SyncSortOrderModel item in dictionary2[key2])
				{
					if (item.Deleted == 1)
					{
						if (item.SyncStatus == 1 || item.SyncStatus == 3)
						{
							list2.Add(new SyncSortOrder
							{
								Id = item.EntityId,
								Order = item.SortOrder,
								Type = item.Type
							});
						}
					}
					else if (item.SyncStatus == 0 || item.SyncStatus == 1 || item.SyncStatus == 3)
					{
						list.Add(new SyncSortOrder
						{
							Id = item.EntityId,
							Order = item.SortOrder,
							Type = item.Type
						});
					}
				}
				if (list.Count > 0 || list2.Count > 0)
				{
					SortOrderData value = new SortOrderData
					{
						Changed = list,
						Deleted = list2
					};
					dictionary3.Add(key2, value);
				}
			}
			if (dictionary3.Count > 0)
			{
				commitBeanMap.Add(key, dictionary3);
			}
		}
		return commitBeanMap;
	}
}
