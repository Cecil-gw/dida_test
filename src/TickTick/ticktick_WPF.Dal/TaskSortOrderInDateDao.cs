using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Dal;

public static class TaskSortOrderInDateDao
{
	public static async Task<List<TaskSortOrderInDateModel>> GetOrderByProjectAndDate(string projectId, string date)
	{
		if (projectId.Contains("_special_id_"))
		{
			projectId = "all";
		}
		return await (from v in App.Connection.Table<TaskSortOrderInDateModel>()
			where v.userid == LocalSettings.Settings.LoginUserId && v.projectid == projectId && v.date == date && v.deleted != 1
			select v).ToListAsync();
	}

	public static async Task<List<TaskSortOrderInDateModel>> GetSortOrderInDateByProjectId(string projectId)
	{
		if (projectId.Contains("_special_id_"))
		{
			projectId = "all";
		}
		return await (from v in App.Connection.Table<TaskSortOrderInDateModel>()
			where v.userid == LocalSettings.Settings.LoginUserId && v.projectid == projectId && v.deleted != 1
			select v).ToListAsync();
	}

	public static async Task<List<TaskSortOrderInDateModel>> GetOrderByDateWithDeleted(string date)
	{
		return await (from v in App.Connection.Table<TaskSortOrderInDateModel>()
			where v.userid == LocalSettings.Settings.LoginUserId && v.date == date
			select v).ToListAsync();
	}

	public static async Task UpdateOrInsertTaskSortOrderInDateAsync(TaskSortOrderInDateModel taskSortOrderInDate)
	{
		if (taskSortOrderInDate.projectid.Contains("_special_id_"))
		{
			taskSortOrderInDate.projectid = "all";
		}
		List<TaskSortOrderInDateModel> list = await (from v in App.Connection.Table<TaskSortOrderInDateModel>()
			where v.userid == taskSortOrderInDate.userid && v.projectid == taskSortOrderInDate.projectid && v.taskid == taskSortOrderInDate.taskid
			select v).ToListAsync();
		taskSortOrderInDate.modifiedTime = Utils.GetNowTimeStamp();
		taskSortOrderInDate.status = 1;
		if (list.Count != 0)
		{
			taskSortOrderInDate._Id = list[0]._Id;
			await App.Connection.UpdateAsync(taskSortOrderInDate);
		}
		else
		{
			taskSortOrderInDate.status = 0;
			await App.Connection.InsertAsync(taskSortOrderInDate);
		}
	}

	public static async Task DeleteTaskSortOrderInDateDbByProjectIdAsync(string id)
	{
		string text = (id.Contains("_special_id_") ? "all" : id);
		await App.Connection.ExecuteAsync("UPDATE TaskSortOrderInDateModel SET modifiedTime = ?, deleted = ?, status = ? WHERE projectid = ?", Utils.GetNowTimeStamp(), 1, 1, text);
	}

	private static async Task<int> DeleteTaskSortOrder(TaskSortOrderInDateModel item)
	{
		return await App.Connection.ExecuteAsync("DELETE FROM TaskSortOrderInDateModel WHERE projectid = ? and taskid = ? and userid = ? and date = ?", item.projectid, item.taskid, item.userid, item.date);
	}

	public static async Task SavePostResult(SyncDataBean<TaskSortOrderInDateModel> dateBean)
	{
		if (dateBean != null)
		{
			if (dateBean.Deleteds != null && dateBean.Deleteds.Count > 0)
			{
				foreach (TaskSortOrderInDateModel deleted in dateBean.Deleteds)
				{
					await DeleteTaskSortOrder(deleted);
				}
			}
			if (dateBean.Updateds != null && dateBean.Updateds.Count > 0)
			{
				foreach (TaskSortOrderInDateModel updated in dateBean.Updateds)
				{
					updated.status = 2;
				}
				await App.Connection.UpdateAllAsync(dateBean.Updateds);
			}
		}
		ClearDeletedOrders();
	}

	private static async Task<int> ClearDeletedOrders()
	{
		return await App.Connection.ExecuteAsync("DELETE FROM TaskSortOrderInDateModel WHERE deleted = ? and status = ?", 1, 2);
	}

	public static async Task SaveRemoteChangesToDb(SyncDataBean<TaskSortOrderInDateModel> pullDataBean)
	{
		foreach (TaskSortOrderInDateModel added in pullDataBean.Addeds)
		{
			await SaveTaskSortOrdersInDate(added);
		}
		foreach (TaskSortOrderInDateModel updated in pullDataBean.Updateds)
		{
			await SaveTaskSortOrdersInDate(updated);
		}
		foreach (TaskSortOrderInDateModel deleted in pullDataBean.Deleteds)
		{
			await DeleteTaskSortOrder(deleted);
		}
	}

	public static async Task<Dictionary<string, Dictionary<string, List<TaskSortOrderInDateModel>>>> GetNeedPostSortOrdersInDateMap()
	{
		Dictionary<string, Dictionary<string, List<TaskSortOrderInDateModel>>> map = new Dictionary<string, Dictionary<string, List<TaskSortOrderInDateModel>>>();
		foreach (TaskSortOrderInDateModel item in await GetNeedPostSortOrdersInDate(long.MaxValue))
		{
			Dictionary<string, List<TaskSortOrderInDateModel>> dictionary = null;
			if (map.ContainsKey(item.date))
			{
				dictionary = map[item.date];
			}
			if (dictionary == null)
			{
				dictionary = new Dictionary<string, List<TaskSortOrderInDateModel>>();
				List<TaskSortOrderInDateModel> value = new List<TaskSortOrderInDateModel> { item };
				dictionary.Add(item.projectid, value);
				map.Add(item.date, dictionary);
			}
			else if (dictionary.ContainsKey(item.projectid))
			{
				dictionary[item.projectid].Add(item);
			}
			else
			{
				List<TaskSortOrderInDateModel> value2 = new List<TaskSortOrderInDateModel> { item };
				dictionary.Add(item.projectid, value2);
			}
		}
		return map;
	}

	public static async Task<List<TaskSortOrderInDateModel>> GetNeedPostSortOrdersInDate(long topPoint)
	{
		return await Task.Run(async () => await (from v in App.Connection.Table<TaskSortOrderInDateModel>()
			where v.userid == LocalSettings.Settings.LoginUserId && v.modifiedTime <= topPoint && v.status != 2
			select v).ToListAsync());
	}

	private static async Task SaveTaskSortOrdersInDate(TaskSortOrderInDateModel orderInDate)
	{
		if (orderInDate != null)
		{
			orderInDate.modifiedTime = Utils.GetNowTimeStamp();
			await DeleteTaskSortOrder(orderInDate);
			await App.Connection.InsertAsync(orderInDate);
		}
	}

	public static async Task BatchInsertOrderInDate(List<TaskSortOrderInDateModel> orderInDate)
	{
		await App.Connection.InsertAllAsync(orderInDate);
	}

	public static async Task<List<TaskSortOrderInDateModel>> GetAllSortsInProject(string projectId)
	{
		return await (from model in App.Connection.Table<TaskSortOrderInDateModel>()
			where model.projectid == projectId && model.deleted == 0
			select model).ToListAsync();
	}

	public static async Task<List<TaskSortOrderInDateModel>> GetAllSorts()
	{
		return await (from model in App.Connection.Table<TaskSortOrderInDateModel>()
			where model.userid == LocalSettings.Settings.LoginUserId
			select model).ToListAsync();
	}

	public static async Task TryAddSplitTaskOrder(string projectId, string date, string baseTaskId, string newTaskId, bool below)
	{
		List<TaskSortOrderInDateModel> list = await GetOrderByProjectAndDate(projectId, date);
		if (list == null || list.Count == 0)
		{
			return;
		}
		List<TaskSortOrderInDateModel> list2 = list.OrderBy((TaskSortOrderInDateModel o) => o.sortOrder).ToList();
		int num = -1;
		for (int num2 = 0; num2 < list2.Count; num2++)
		{
			if (list2[num2].taskid == baseTaskId)
			{
				num = num2;
				break;
			}
		}
		if (num >= 0)
		{
			long sortOrder = ((num == 0 && !below) ? (list2[0].sortOrder - 268435456) : ((!(num == list.Count - 1 && below)) ? ((list2[num].sortOrder + list2[below ? (num + 1) : (num - 1)].sortOrder) / 2) : (list2[list.Count - 1].sortOrder + 268435456)));
			TaskSortOrderInDateModel item = new TaskSortOrderInDateModel
			{
				userid = LocalSettings.Settings.LoginUserId,
				taskid = newTaskId,
				date = date,
				projectid = projectId,
				sortOrder = sortOrder,
				modifiedTime = Utils.GetNowTimeStamp(),
				status = 1,
				type = 1
			};
			await App.Connection.InsertAsync(item);
		}
	}

	public static async Task SyncOnEventCalendarChanged(string originalId, string revisedId)
	{
		await App.Connection.ExecuteAsync("UPDATE TaskSortOrderInDateModel SET taskid=?, status=? WHERE taskid=?", revisedId, 1, originalId);
	}

	public static async Task UpdateSortOrderInDate(string id, long order, string date, string sortProjectId, int type = 1)
	{
		id = ArchivedDao.GetOriginalId(id);
		TaskSortOrderInDateModel taskSortOrderInDateModel = await (from m in App.Connection.Table<TaskSortOrderInDateModel>()
			where m.projectid == sortProjectId && id == m.taskid && m.date == date && m.deleted == 0
			select m).FirstOrDefaultAsync();
		if (taskSortOrderInDateModel != null)
		{
			taskSortOrderInDateModel.sortOrder = order;
			taskSortOrderInDateModel.modifiedTime = Utils.GetNowTimeStamp();
			taskSortOrderInDateModel.status = 1;
			taskSortOrderInDateModel.type = type;
			await App.Connection.UpdateAsync(taskSortOrderInDateModel);
			return;
		}
		TaskSortOrderInDateModel item = new TaskSortOrderInDateModel
		{
			userid = LocalSettings.Settings.LoginUserId,
			taskid = id,
			date = date,
			projectid = sortProjectId,
			sortOrder = order,
			modifiedTime = Utils.GetNowTimeStamp(),
			status = 1,
			type = type
		};
		await App.Connection.InsertAsync(item);
	}

	public static async Task<List<TaskSortOrderInDateModel>> BatchResetSortOrder(List<(string, DisplayType)> id2Types, string date, string sortProjectId)
	{
		List<TaskSortOrderInDateModel> result = new List<TaskSortOrderInDateModel>();
		if (id2Types == null || id2Types.Count == 0)
		{
			return result;
		}
		List<string> ids = id2Types.Select(((string, DisplayType) i) => ArchivedDao.GetOriginalId(i.Item1)).ToList();
		List<TaskSortOrderInDateModel> models = await (from m in App.Connection.Table<TaskSortOrderInDateModel>()
			where m.projectid == sortProjectId && ids.Contains(m.taskid) && m.date == date && m.deleted == 0
			select m).ToListAsync();
		long sortOrder = 0L;
		foreach (var id2Type in id2Types)
		{
			string id = ArchivedDao.GetOriginalId(id2Type.Item1);
			TaskSortOrderInDateModel model = models.FirstOrDefault((TaskSortOrderInDateModel m) => m.taskid == id);
			if (model != null)
			{
				model.sortOrder = sortOrder;
				model.modifiedTime = Utils.GetNowTimeStamp();
				model.status = 1;
				model.type = EntityType.GetEntityTypeNum(id2Type.Item2);
				await App.Connection.UpdateAsync(model);
			}
			else
			{
				model = new TaskSortOrderInDateModel
				{
					userid = LocalSettings.Settings.LoginUserId,
					taskid = ArchivedDao.GetOriginalId(id2Type.Item1),
					date = date,
					projectid = sortProjectId,
					sortOrder = sortOrder,
					modifiedTime = Utils.GetNowTimeStamp(),
					status = 1,
					type = EntityType.GetEntityTypeNum(id2Type.Item2)
				};
				await App.Connection.InsertAsync(model);
			}
			sortOrder += 268435456;
			result.Add(model);
		}
		return result;
	}

	public static async Task NewTaskAdded(TaskModel task, string catId)
	{
		if (task == null)
		{
			return;
		}
		if (catId.Contains("_special_id_"))
		{
			catId = "all";
		}
		DateTime? date = DateUtils.GetDateSectionDate(DateUtils.GetSectionCategory(task.startDate, task.dueDate, task.isAllDay));
		if (!date.HasValue || date.Value < DateTime.Today)
		{
			return;
		}
		List<TaskSortOrderInDateModel> list = await GetOrderByProjectAndDate(catId, date.Value.ToString("yyyyMMdd", App.Ci));
		if (list == null || list.Count == 0)
		{
			return;
		}
		TaskDefaultModel defaultSafely = TaskDefaultDao.GetDefaultSafely();
		TaskSortOrderInDateModel taskSortOrderInDateModel = new TaskSortOrderInDateModel
		{
			userid = LocalSettings.Settings.LoginUserId,
			taskid = task.id,
			date = date.Value.ToString("yyyyMMdd", App.Ci),
			projectid = catId,
			modifiedTime = Utils.GetNowTimeStamp(),
			status = 0,
			type = 1
		};
		if (defaultSafely.AddTo == 0)
		{
			long val = list.Select((TaskSortOrderInDateModel o) => o.sortOrder).Min();
			val = Math.Min(0L, val);
			taskSortOrderInDateModel.sortOrder = val - 268435456;
		}
		else
		{
			long val2 = list.Select((TaskSortOrderInDateModel o) => o.sortOrder).Max();
			val2 = Math.Max(0L, val2);
			taskSortOrderInDateModel.sortOrder = val2 + 268435456;
		}
		await App.Connection.InsertAsync(taskSortOrderInDateModel);
	}
}
