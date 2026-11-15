using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class TaskSyncedJsonDao
{
	public static async Task<List<TaskSyncedJsonModel>> GetTaskSyncModels()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<TaskSyncedJsonModel>()
			where model.userId == userId
			select model).ToListAsync();
	}

	public static async Task<TaskSyncedJsonModel> GetJsonByTaskId(string id)
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		string sql = "select * from TaskSyncedJsonModel where taskSid  = '" + id + "' and userId = '" + loginUserId + "'";
		List<TaskSyncedJsonModel> list = await App.Connection.QueryAsync<TaskSyncedJsonModel>(sql, Array.Empty<object>());
		if (list != null && list.Count > 0)
		{
			for (int i = 1; i < list.Count; i++)
			{
				App.Connection.DeleteAsync(list[i]);
			}
			return list[0];
		}
		return null;
	}

	public static async Task<IEnumerable<string>> GetJsonNotExitIds()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		string sql = "select TaskModel.id as Id from TaskModel left join TaskSyncedJsonModel on TaskModel.id=TaskSyncedJsonModel.taskSid " + $"where TaskModel.deleted = {0} and TaskModel.userid = '{loginUserId}' and TaskSyncedJsonModel.taskSid is null";
		return (await App.Connection.QueryAsync<IdModel>(sql, Array.Empty<object>()))?.Select((IdModel i) => i.Id);
	}

	public static async Task SaveTaskSyncedJsons(TaskSyncedJsonBean bean)
	{
		if (bean.Empty)
		{
			return;
		}
		List<TaskSyncedJsonModel> addlist;
		if (bean.Added.Count > 0)
		{
			addlist = new List<TaskSyncedJsonModel>();
			foreach (TaskModel item3 in bean.Added)
			{
				TaskSyncedJsonModel item = new TaskSyncedJsonModel
				{
					taskSid = item3.id,
					userId = item3.userId,
					jsonString = JsonConvert.SerializeObject(item3)
				};
				addlist.Add(item);
			}
			if (addlist.Count > 0)
			{
				await App.Connection.InsertAllAsync(addlist);
				foreach (TaskSyncedJsonModel item4 in addlist)
				{
					CacheManager.UpdateSyncTask(item4);
				}
			}
		}
		if (bean.Updated.Count <= 0)
		{
			return;
		}
		addlist = new List<TaskSyncedJsonModel>();
		List<TaskSyncedJsonModel> updatelist = new List<TaskSyncedJsonModel>();
		foreach (TaskModel task in bean.Updated)
		{
			List<TaskSyncedJsonModel> list = await (from v in App.Connection.Table<TaskSyncedJsonModel>()
				where v.taskSid == task.id
				select v).ToListAsync();
			if (list.Count > 0)
			{
				TaskSyncedJsonModel taskSyncedJsonModel = list[0];
				TaskSyncedJsonModel item2 = new TaskSyncedJsonModel
				{
					_Id = taskSyncedJsonModel._Id,
					taskSid = task.id,
					userId = task.userId,
					jsonString = JsonConvert.SerializeObject(task)
				};
				updatelist.Add(item2);
			}
			else
			{
				addlist.Add(new TaskSyncedJsonModel
				{
					taskSid = task.id,
					userId = task.userId,
					jsonString = JsonConvert.SerializeObject(task)
				});
			}
		}
		if (updatelist.Count > 0)
		{
			await App.Connection.UpdateAllAsync(updatelist);
			foreach (TaskSyncedJsonModel item5 in updatelist)
			{
				CacheManager.UpdateSyncTask(item5);
			}
		}
		if (addlist.Count <= 0)
		{
			return;
		}
		await App.Connection.InsertAllAsync(addlist);
		foreach (TaskSyncedJsonModel item6 in addlist)
		{
			CacheManager.UpdateSyncTask(item6);
		}
	}

	public static async Task DeleteTaskSyncedJsonPhysical(string taskSid)
	{
		List<TaskSyncedJsonModel> list = await (from task in App.Connection.Table<TaskSyncedJsonModel>()
			where task.taskSid == taskSid
			select task).ToListAsync();
		if (list.Count <= 0)
		{
			return;
		}
		foreach (TaskSyncedJsonModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}
}
