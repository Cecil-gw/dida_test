using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class TaskOrderBatchHandler : BatchHandler
{
	private SyncResult _syncResult;

	public TaskOrderBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
		_syncResult = syncResult;
	}

	public async Task MergeWithServer(SyncTaskOrderBean syncTaskOrderBean)
	{
		if (syncTaskOrderBean != null && (syncTaskOrderBean.taskOrderByDate != null || syncTaskOrderBean.taskOrderByPriority != null || syncTaskOrderBean.taskOrderByProject != null))
		{
			bool flag = await MergeTaskOrderByDate(syncTaskOrderBean.taskOrderByDate);
			if (!flag)
			{
				flag = await MergeTaskOrderByPriority(syncTaskOrderBean.taskOrderByPriority);
			}
			bool flag2 = flag;
			if (!flag2)
			{
				flag2 = await MergeTaskOrderByProject(syncTaskOrderBean.taskOrderByProject);
			}
			if (flag2)
			{
				_syncResult.TaskSortChanged = true;
			}
		}
	}

	private async Task<bool> MergeTaskOrderByProject(Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> taskOrderByProject)
	{
		if (taskOrderByProject == null || !taskOrderByProject.Any())
		{
			return false;
		}
		string log = "MergeTaskOrderByProject:" + taskOrderByProject.Count + " ,  ";
		SyncDataBean<TaskSortOrderInProjectModel> pullDataBean = new SyncDataBean<TaskSortOrderInProjectModel>();
		List<TaskSortOrderInProjectModel> source = await ProjectSortOrderDao.GetLocalSortOrders();
		List<string> deletedProjects = new List<string>();
		foreach (string key in taskOrderByProject.Keys)
		{
			log = log + key + ": (";
			foreach (KeyValuePair<string, SyncTaskOrderByDateBean> item in taskOrderByProject[key])
			{
				SyncTaskOrderByDateBean value = item.Value;
				foreach (TaskSortOrder order in value.changed)
				{
					TaskSortOrderInProjectModel taskSortOrderInProjectModel = new TaskSortOrderInProjectModel
					{
						UserId = userId,
						ProjectId = key,
						EntityType = EntityType.GetEntityType(order.type),
						EntityId = order.id,
						SortOrder = order.order.GetValueOrDefault(),
						SyncStatus = 2
					};
					TaskSortOrderInProjectModel taskSortOrderInProjectModel2 = source.FirstOrDefault((TaskSortOrderInProjectModel o) => o.ProjectId == key && o.EntityId == order.id);
					if (taskSortOrderInProjectModel2 == null)
					{
						pullDataBean.Addeds.Add(taskSortOrderInProjectModel);
					}
					else if (taskSortOrderInProjectModel.SortOrder != taskSortOrderInProjectModel2.SortOrder)
					{
						taskSortOrderInProjectModel2.SortOrder = taskSortOrderInProjectModel.SortOrder;
						pullDataBean.Updateds.Add(taskSortOrderInProjectModel2);
					}
				}
				if (!value.changed.Any() && !value.deleted.Any())
				{
					log = log + item.Key + $":deleted {value.deleted?.Count}, changed {value.changed?.Count}";
					deletedProjects.Add(key);
				}
			}
			log += ")  ";
		}
		await ProjectSortOrderDao.SaveRemoteChangesToDb(pullDataBean);
		if (deletedProjects.Any())
		{
			await ProjectSortOrderDao.DeleteRemoteDeleted(deletedProjects);
		}
		log += $"a: {pullDataBean.Addeds.Count} , u: {pullDataBean.Updateds.Count}, d: {pullDataBean.Deleteds.Count} ,deletedProjects : {deletedProjects.Count}";
		UtilLog.Info(log);
		return !pullDataBean.IsEmpty() || deletedProjects.Any();
	}

	private async Task<bool> MergeTaskOrderByPriority(Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> taskOrderByPriority)
	{
		if (taskOrderByPriority == null || !taskOrderByPriority.Any())
		{
			return false;
		}
		string log = "MergeTaskOrderByPriority:" + taskOrderByPriority.Count + " ,  ";
		SyncDataBean<TaskSortOrderInPriorityModel> pullDataBean = new SyncDataBean<TaskSortOrderInPriorityModel>();
		List<TaskSortOrderInPriorityModel> source = await TaskSortOrderInPriorityDao.GetAllSortOrders();
		List<string> deletedProjects = new List<string>();
		foreach (string key in taskOrderByPriority.Keys)
		{
			log = log + key + ": (";
			foreach (KeyValuePair<string, SyncTaskOrderByDateBean> group in taskOrderByPriority[key])
			{
				SyncTaskOrderByDateBean value = group.Value;
				foreach (TaskSortOrder order in value.changed)
				{
					TaskSortOrderInPriorityModel taskSortOrderInPriorityModel = new TaskSortOrderInPriorityModel
					{
						UserId = userId,
						CatId = group.Key,
						Priority = int.Parse(key),
						EntityType = EntityType.GetEntityType(order.type),
						EntityId = order.id,
						SortOrder = order.order.GetValueOrDefault(),
						SyncStatus = 2
					};
					List<TaskSortOrderInPriorityModel> list = source.Where((TaskSortOrderInPriorityModel o) => o.Priority.ToString() == key && o.CatId == group.Key && o.EntityId == order.id).ToList();
					if (list.Count == 0)
					{
						pullDataBean.Addeds.Add(taskSortOrderInPriorityModel);
						continue;
					}
					TaskSortOrderInPriorityModel taskSortOrderInPriorityModel2 = list[0];
					if (taskSortOrderInPriorityModel.SortOrder != taskSortOrderInPriorityModel2.SortOrder)
					{
						taskSortOrderInPriorityModel2.SortOrder = taskSortOrderInPriorityModel.SortOrder;
						pullDataBean.Updateds.Add(taskSortOrderInPriorityModel2);
					}
					for (int num = 1; num < list.Count; num++)
					{
						pullDataBean.Deleteds.Add(list[num]);
					}
				}
				if (!value.changed.Any() && !value.deleted.Any())
				{
					log = log + group.Key + $":deleted {value.deleted?.Count}, changed {value.changed?.Count}";
					deletedProjects.Add(group.Key);
				}
			}
			log += ")  ";
		}
		await TaskSortOrderInPriorityDao.SaveRemoteChanges(pullDataBean);
		if (deletedProjects.Any())
		{
			await TaskSortOrderInPriorityDao.DeleteRemoteDeleted(deletedProjects);
		}
		log += $"a: {pullDataBean.Addeds.Count} , u: {pullDataBean.Updateds.Count}, d: {pullDataBean.Deleteds.Count} ,deletedProjects : {deletedProjects.Count}";
		UtilLog.Info(log);
		return !pullDataBean.IsEmpty() || deletedProjects.Any();
	}

	private async Task<bool> MergeTaskOrderByDate(Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> taskOrderByDate)
	{
		if (taskOrderByDate == null || taskOrderByDate.Count == 0)
		{
			return false;
		}
		string log = "MergeTaskOrderByDate:" + taskOrderByDate.Count + " ,  ";
		SyncDataBean<TaskSortOrderInDateModel> pullDataBean = new SyncDataBean<TaskSortOrderInDateModel>();
		foreach (string dateStr in taskOrderByDate.Keys)
		{
			log = log + dateStr + ": (";
			Dictionary<string, List<TaskSortOrderInDateModel>> dictionary = AssemblyOrderDict(await TaskSortOrderInDateDao.GetOrderByDateWithDeleted(dateStr));
			Dictionary<string, SyncTaskOrderByDateBean> dictionary2 = taskOrderByDate[dateStr];
			foreach (string key in dictionary2.Keys)
			{
				SyncTaskOrderByDateBean syncTaskOrderByDateBean = dictionary2[key];
				if (dictionary.ContainsKey(key))
				{
					List<TaskSortOrderInDateModel> list = dictionary[key];
					List<TaskSortOrder> deleted = syncTaskOrderByDateBean.deleted;
					if (deleted != null && deleted.Count < 1)
					{
						List<TaskSortOrder> changed = syncTaskOrderByDateBean.changed;
						if (changed != null && changed.Count < 1 && list != null && list.Count > 0)
						{
							pullDataBean.Deleteds.AddRange(list);
						}
					}
					MergeDeletedOrderByDate(syncTaskOrderByDateBean.deleted, list, pullDataBean);
					MergeChangedOrderByDate(syncTaskOrderByDateBean.changed, list, pullDataBean, dateStr, key);
				}
				else
				{
					MergeChangedOrderByDate(syncTaskOrderByDateBean.changed, new List<TaskSortOrderInDateModel>(), pullDataBean, dateStr, key);
				}
				log = log + key + $":deleted {syncTaskOrderByDateBean.deleted?.Count}, changed {syncTaskOrderByDateBean.changed?.Count}";
			}
			log += ")  ";
		}
		await TaskSortOrderInDateDao.SaveRemoteChangesToDb(pullDataBean);
		log += $"a: {pullDataBean.Addeds.Count} , u: {pullDataBean.Updateds.Count}, d: {pullDataBean.Deleteds.Count} ";
		UtilLog.Info(log);
		return !pullDataBean.IsEmpty();
	}

	private static Dictionary<string, List<TaskSortOrderInDateModel>> AssemblyOrderDict(IEnumerable<TaskSortOrderInDateModel> orders)
	{
		Dictionary<string, List<TaskSortOrderInDateModel>> dictionary = new Dictionary<string, List<TaskSortOrderInDateModel>>();
		foreach (TaskSortOrderInDateModel order in orders)
		{
			if (!dictionary.ContainsKey(order.projectid))
			{
				dictionary.Add(order.projectid, new List<TaskSortOrderInDateModel> { order });
			}
			else
			{
				dictionary[order.projectid].Add(order);
			}
		}
		return dictionary;
	}

	public static async Task HandleCommitResult(BatchTaskOrderUpdateResult result, long lastPostPoint)
	{
		await HandleCommitOrderByDateResult(result.TaskOrderByDate, lastPostPoint);
		await HandleCommitOrderByPriorityResult();
		await HandleCommitOrderByProjectResult();
	}

	private static async Task HandleCommitOrderByProjectResult()
	{
		await SaveCommitOrderByProjectResult();
	}

	private static async Task SaveCommitOrderByProjectResult()
	{
		SyncDataBean<TaskSortOrderInProjectModel> projectBean = new SyncDataBean<TaskSortOrderInProjectModel>();
		List<TaskSortOrderInProjectModel> list = await ProjectSortOrderDao.GetNeedPostSortOrdersInProject();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TaskSortOrderInProjectModel item in list)
		{
			if (item.SyncStatus != 2)
			{
				projectBean.Updateds.Add(item);
			}
			else
			{
				projectBean.Deleteds.Add(item);
			}
		}
		await ProjectSortOrderDao.SavePostResult(projectBean);
	}

	private static async Task HandleCommitOrderByPriorityResult()
	{
		await SaveCommitOrderByPriorityResult();
	}

	private static async Task SaveCommitOrderByPriorityResult()
	{
		SyncDataBean<TaskSortOrderInPriorityModel> dateBean = new SyncDataBean<TaskSortOrderInPriorityModel>();
		List<TaskSortOrderInPriorityModel> list = await TaskSortOrderInPriorityDao.GetNeedPostSortOrders();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TaskSortOrderInPriorityModel item in list)
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
		await TaskSortOrderInPriorityDao.SavePostResult(dateBean);
	}

	private static async Task<bool> HandleCommitOrderByDateResult(BatchUpdateResult result, long lastPostPoint)
	{
		List<string> errorList = new List<string>();
		if (result.Id2error != null && result.Id2error.Count > 0)
		{
			errorList.AddRange(result.Id2error.Keys);
		}
		await SaveCommitOrderByDateResult(errorList, lastPostPoint);
		return errorList.Count == 0;
	}

	private static async Task SaveCommitOrderByDateResult(ICollection<string> errorIds, long lastPostPoint)
	{
		SyncDataBean<TaskSortOrderInDateModel> dateBean = new SyncDataBean<TaskSortOrderInDateModel>();
		List<TaskSortOrderInDateModel> list = await TaskSortOrderInDateDao.GetNeedPostSortOrdersInDate(lastPostPoint);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (TaskSortOrderInDateModel item in list)
		{
			if (!errorIds.Contains(item.date))
			{
				if (item.status != 2)
				{
					dateBean.Updateds.Add(item);
				}
				else
				{
					dateBean.Deleteds.Add(item);
				}
			}
		}
		await TaskSortOrderInDateDao.SavePostResult(dateBean);
	}

	public static async Task<SyncTaskOrderBean> CreateCommitBean()
	{
		SyncTaskOrderBean syncTaskOrderBean = new SyncTaskOrderBean();
		SyncTaskOrderBean syncTaskOrderBean2 = syncTaskOrderBean;
		syncTaskOrderBean2.taskOrderByDate = await CreateCommitOrderByDateMap();
		SyncTaskOrderBean syncTaskOrderBean3 = syncTaskOrderBean;
		syncTaskOrderBean3.taskOrderByPriority = await CreateCommitOrderByPriorityMap();
		SyncTaskOrderBean syncTaskOrderBean4 = syncTaskOrderBean;
		syncTaskOrderBean4.taskOrderByProject = await CreateCommitOrderByProjectMap();
		return syncTaskOrderBean;
	}

	private static async Task<Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>>> CreateCommitOrderByProjectMap()
	{
		Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> commitBeanMap = new Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>>();
		Dictionary<string, Dictionary<string, List<TaskSortOrderInProjectModel>>> dictionary = await ProjectSortOrderDao.GetNeedPostSortOrderInProjectMap();
		if (dictionary == null || dictionary.Count == 0)
		{
			return commitBeanMap;
		}
		foreach (string key in dictionary.Keys)
		{
			Dictionary<string, List<TaskSortOrderInProjectModel>> dictionary2 = dictionary[key];
			Dictionary<string, SyncTaskOrderByDateBean> dictionary3 = new Dictionary<string, SyncTaskOrderByDateBean>();
			foreach (string key2 in dictionary2.Keys)
			{
				List<TaskSortOrder> list = new List<TaskSortOrder>();
				List<TaskSortOrder> list2 = new List<TaskSortOrder>();
				foreach (TaskSortOrderInProjectModel item in dictionary2[key2])
				{
					if (item.Deleted == 1)
					{
						if (item.SyncStatus == 1)
						{
							list2.Add(TaskSortOrderTransfer.ConvertLocalToRemote(item));
						}
					}
					else
					{
						list.Add(TaskSortOrderTransfer.ConvertLocalToRemote(item));
					}
				}
				if (list.Count > 0 || list2.Count > 0)
				{
					SyncTaskOrderByDateBean value = new SyncTaskOrderByDateBean
					{
						changed = list,
						deleted = list2
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

	private static async Task<Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>>> CreateCommitOrderByPriorityMap()
	{
		Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> commitBeanMap = new Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>>();
		Dictionary<string, Dictionary<string, List<TaskSortOrderInPriorityModel>>> dictionary = await TaskSortOrderInPriorityDao.GetNeedPostOrdersMap();
		if (dictionary == null || dictionary.Count == 0)
		{
			return commitBeanMap;
		}
		foreach (string key in dictionary.Keys)
		{
			Dictionary<string, List<TaskSortOrderInPriorityModel>> dictionary2 = dictionary[key];
			Dictionary<string, SyncTaskOrderByDateBean> dictionary3 = new Dictionary<string, SyncTaskOrderByDateBean>();
			foreach (string key2 in dictionary2.Keys)
			{
				List<TaskSortOrder> list = new List<TaskSortOrder>();
				List<TaskSortOrder> list2 = new List<TaskSortOrder>();
				foreach (TaskSortOrderInPriorityModel item in dictionary2[key2])
				{
					if (item.Deleted == 1)
					{
						if (item.SyncStatus == 1)
						{
							list2.Add(TaskSortOrderTransfer.ConvertLocalToRemote(item));
						}
					}
					else
					{
						list.Add(TaskSortOrderTransfer.ConvertLocalToRemote(item));
					}
				}
				if (list.Count > 0 || list2.Count > 0)
				{
					SyncTaskOrderByDateBean value = new SyncTaskOrderByDateBean
					{
						changed = list,
						deleted = list2
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

	private static async Task<Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>>> CreateCommitOrderByDateMap()
	{
		Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>> commitBeanMap = new Dictionary<string, Dictionary<string, SyncTaskOrderByDateBean>>();
		Dictionary<string, Dictionary<string, List<TaskSortOrderInDateModel>>> dictionary = await TaskSortOrderInDateDao.GetNeedPostSortOrdersInDateMap();
		if (dictionary == null || dictionary.Count == 0)
		{
			return commitBeanMap;
		}
		foreach (string key in dictionary.Keys)
		{
			Dictionary<string, List<TaskSortOrderInDateModel>> dictionary2 = dictionary[key];
			Dictionary<string, SyncTaskOrderByDateBean> dictionary3 = new Dictionary<string, SyncTaskOrderByDateBean>();
			foreach (string key2 in dictionary2.Keys)
			{
				List<TaskSortOrder> list = new List<TaskSortOrder>();
				List<TaskSortOrder> list2 = new List<TaskSortOrder>();
				foreach (TaskSortOrderInDateModel item in dictionary2[key2])
				{
					if (item.deleted == 1)
					{
						if (item.status == 1)
						{
							list2.Add(TaskSortOrderTransfer.ConvertLocalToRemote(item));
						}
					}
					else
					{
						list.Add(TaskSortOrderTransfer.ConvertLocalToRemote(item));
					}
				}
				if (list.Count > 0 || list2.Count > 0)
				{
					SyncTaskOrderByDateBean value = new SyncTaskOrderByDateBean
					{
						changed = list,
						deleted = list2
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

	private void MergeChangedOrderByDate(IReadOnlyCollection<TaskSortOrder> changes, List<TaskSortOrderInDateModel> localOrders, SyncDataBean<TaskSortOrderInDateModel> pullDataBean, string dateStr, string projectId)
	{
		if (changes == null || changes.Count == 0)
		{
			return;
		}
		foreach (TaskSortOrder change in changes)
		{
			TaskSortOrderInDateModel taskSortOrderInDateModel = localOrders.FirstOrDefault((TaskSortOrderInDateModel order) => order.taskid == change.id);
			if (taskSortOrderInDateModel == null)
			{
				TaskSortOrderInDateModel item = new TaskSortOrderInDateModel
				{
					userid = userId,
					date = dateStr,
					projectid = projectId,
					taskid = change.id,
					sortOrder = change.order.GetValueOrDefault(),
					status = 2
				};
				pullDataBean.Addeds.Add(item);
			}
			else if (taskSortOrderInDateModel.status == 2)
			{
				taskSortOrderInDateModel.sortOrder = change.order.GetValueOrDefault();
				taskSortOrderInDateModel.status = 2;
				pullDataBean.Updateds.Add(taskSortOrderInDateModel);
			}
		}
	}

	private static void MergeDeletedOrderByDate(IReadOnlyCollection<TaskSortOrder> deleteds, List<TaskSortOrderInDateModel> localOrders, SyncDataBean<TaskSortOrderInDateModel> pullDataBean)
	{
		if (deleteds == null || deleteds.Count == 0 || localOrders == null)
		{
			return;
		}
		foreach (TaskSortOrder deleted in deleteds)
		{
			TaskSortOrderInDateModel taskSortOrderInDateModel = localOrders.FirstOrDefault((TaskSortOrderInDateModel order) => order.taskid == deleted.id);
			if (taskSortOrderInDateModel != null)
			{
				pullDataBean.Deleteds.Add(taskSortOrderInDateModel);
			}
		}
	}
}
