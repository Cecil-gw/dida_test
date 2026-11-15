using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Dal;

public class PomoDao : BaseDao<PomodoroModel>
{
	public static async Task<List<PomodoroModel>> GetAllPomo(bool containDeleted = false)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		AsyncTableQuery<PomodoroModel> asyncTableQuery = from x in App.Connection.Table<PomodoroModel>()
			where x.UserId == userId
			select x;
		if (!containDeleted)
		{
			asyncTableQuery = asyncTableQuery.Where((PomodoroModel x) => x.SyncStatus != -1);
		}
		return await asyncTableQuery.ToListAsync();
	}

	public static async Task<List<PomodoroModel>> GetPomoDescByStart(int limit)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from x in App.Connection.Table<PomodoroModel>()
			where x.UserId == userId
			where x.SyncStatus != -1
			orderby x.StartTime descending
			select x).Take(limit).ToListAsync();
	}

	public static async Task<List<PomodoroModel>> GetPomoDescByStartAndTimerId(int limit, string timerId, string objId, bool isHabit, DateTime startTime, DateTime endTime)
	{
		string text = "";
		if (!string.IsNullOrEmpty(objId))
		{
			text = (isHabit ? (" or b.HabitId = '" + objId + "' ") : (" or b.TaskId = '" + objId + "' "));
		}
		string text2 = startTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss", App.Ci);
		string text3 = endTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss", App.Ci);
		string text4 = "Select distinct a.Id as Id,a.Status as Status,a.StartTime as StartTime,a.EndTime as EndTime,a.AdjustTime as AdjustTime, a.SyncStatus as SyncStatus , a.Type as Type,a.Note as Note,a.PauseDuration as PauseDuration from PomodoroModel a,PomoTask b " + $"where a.Id = b.PomoId and a.UserId = '{LocalSettings.Settings.LoginUserId}' and ( b.TimerSid = '{timerId}' {text} ) and a.SyncStatus != {-1} and " + "(b.StartTime >= '" + text2 + "' and b.StartTime < '" + text3 + "' or b.EndTime > '" + text2 + "' and b.EndTime <= '" + text3 + "')";
		text4 += " order by StartTime  desc ";
		if (limit != 0)
		{
			text4 += $" limit {limit} ";
		}
		return await App.Connection.QueryAsync<PomodoroModel>(text4, Array.Empty<object>());
	}

	public static async Task<List<PomodoroModel>> GetPomoByDateSpan(DateTime startTime, DateTime endTime, bool containDeleted = false)
	{
		if (startTime > endTime)
		{
			return new List<PomodoroModel>();
		}
		try
		{
			string userId = LocalSettings.Settings.LoginUserId;
			AsyncTableQuery<PomodoroModel> asyncTableQuery = from x in App.Connection.Table<PomodoroModel>()
				where x.UserId == userId
				select x;
			if (!containDeleted)
			{
				asyncTableQuery = asyncTableQuery.Where((PomodoroModel x) => x.SyncStatus != -1);
			}
			asyncTableQuery = asyncTableQuery.Where((PomodoroModel p) => (p.StartTime >= startTime && p.StartTime < endTime) || (p.EndTime > startTime && p.EndTime <= endTime) || (p.StartTime <= startTime && p.EndTime >= endTime));
			return await asyncTableQuery.ToListAsync();
		}
		catch (Exception ex)
		{
			UtilLog.Info($"GetPomoByDateSpanError start: {Utils.GetSafeDateLogText(startTime)}, end: {Utils.GetSafeDateLogText(endTime)}, ci: {Thread.CurrentThread.CurrentCulture}, error: {ex}");
			throw;
		}
	}

	public static async Task<PomodoroModel> GetPomoById(string pomoId)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from x in App.Connection.Table<PomodoroModel>()
			where x.UserId == userId && x.Id == pomoId
			select x).FirstOrDefaultAsync();
	}

	public static async Task<List<PomodoroModel>> GetPomoInIds(List<string> pomoIds)
	{
		if (pomoIds == null)
		{
			return new List<PomodoroModel>();
		}
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from x in App.Connection.Table<PomodoroModel>()
			where x.UserId == userId && pomoIds.Contains(x.Id)
			select x).ToListAsync();
	}

	public static async Task<List<PomodoroModel>> GetNeedPostPomos()
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<PomodoroModel> obj = await (from x in App.Connection.Table<PomodoroModel>()
			where x.UserId == userId && x.SyncStatus != 2
			select x).ToListAsync();
		Dictionary<string, PomodoroModel> dictionary = new Dictionary<string, PomodoroModel>();
		foreach (PomodoroModel item in obj)
		{
			if (dictionary.ContainsKey(item.Id) || Utils.IsEmptyDate(item.StartTime))
			{
				App.Connection.DeleteAsync(item);
			}
			else
			{
				dictionary[item.Id] = item;
			}
		}
		return dictionary.Values.ToList();
	}

	public static async Task<List<PomodoroModel>> GetPomosByType(int? type)
	{
		try
		{
			string userId = LocalSettings.Settings.LoginUserId;
			AsyncTableQuery<PomodoroModel> asyncTableQuery = from x in App.Connection.Table<PomodoroModel>()
				where x.UserId == userId && x.SyncStatus != -1
				select x;
			if (type.HasValue)
			{
				asyncTableQuery = asyncTableQuery.Where((PomodoroModel x) => (int?)x.Type == type);
			}
			return await asyncTableQuery.ToListAsync();
		}
		catch (Exception)
		{
			return new List<PomodoroModel>();
		}
	}

	public static async Task UpdateAllAsync(IEnumerable<PomodoroModel> pomos)
	{
		await App.Connection.UpdateAllAsync(pomos);
	}

	public new static async Task UpdateAsync(PomodoroModel pomo)
	{
		await App.Connection.UpdateAsync(pomo);
	}

	public static async Task DeleteAllAsync(IEnumerable<PomodoroModel> pomos)
	{
		foreach (PomodoroModel pomo in pomos)
		{
			await App.Connection.ExecuteAsync(string.Format("DELETE FROM {0} WHERE _Id={1}", "PomodoroModel", pomo._Id));
		}
	}

	public static async Task<int> DeleteByIdAsync(string pomoId)
	{
		return await App.Connection.ExecuteAsync("DELETE FROM PomodoroModel WHERE Id=?", pomoId);
	}

	public new static async Task DeleteAsync(PomodoroModel pomo)
	{
		await App.Connection.DeleteAsync(pomo);
	}

	public static async Task InsertAllAsync(IEnumerable<PomodoroModel> pomos)
	{
		await App.Connection.InsertAllAsync(pomos);
	}

	public new static async Task InsertAsync(PomodoroModel pomo)
	{
		if (string.IsNullOrEmpty(pomo.UserId))
		{
			pomo.UserId = Utils.GetCurrentUserStr();
		}
		await App.Connection.InsertAsync(pomo);
	}

	public static async Task<List<PomoTask>> GetPomoTasksByPomoId(string pomoId)
	{
		List<PomoTask> list = await (from p in App.Connection.Table<PomoTask>()
			where p.PomoId == pomoId
			select p).ToListAsync();
		IEnumerable<string> enumerable = (from x in list
			where !string.IsNullOrEmpty(x.TaskId)
			select x.TaskId).Distinct();
		if (enumerable.Any())
		{
			Dictionary<string, string> dictionary = (await TaskDao.GetTasksByIds(enumerable)).Where((TaskModel x) => !string.IsNullOrEmpty(x.id)).ToDictionary((TaskModel x) => x.id, (TaskModel x) => x.title);
			foreach (PomoTask item in list)
			{
				if (!string.IsNullOrEmpty(item.TaskId) && dictionary.TryGetValue(item.TaskId, out var value))
				{
					item.Title = value;
				}
			}
		}
		list?.ForEach(delegate(PomoTask t)
		{
			t.Tags = TagSerializer.ToTags(t.TagString).ToArray();
		});
		return list;
	}

	public static async Task<PomoTask> GetPomoTaskByPomoIdAndTime(string pomoId, DateTime start)
	{
		PomoTask data = await (from p in App.Connection.Table<PomoTask>()
			where p.PomoId == pomoId && p.StartTime == start
			select p).FirstOrDefaultAsync();
		if (!string.IsNullOrEmpty(data.TaskId))
		{
			TaskModel taskModel = await TaskDao.GetTaskById(data.TaskId);
			if (taskModel != null)
			{
				data.Title = taskModel.title;
			}
		}
		return data;
	}

	public static async Task UpdatePomoTask(PomoTask pomoTask)
	{
		await App.Connection.UpdateAsync(pomoTask);
	}

	public static async Task UpdateAllPomoTask(IEnumerable<PomoTask> pomos)
	{
		await App.Connection.UpdateAllAsync(pomos);
	}

	public static async Task UpdatePomo(PomodoroModel pomo)
	{
		if (pomo.SyncStatus == 2)
		{
			pomo.SyncStatus = 1;
		}
		await UpdateAsync(pomo);
	}

	public static async Task<List<PomoTask>> GetPomoTasksByTaskId(string taskId)
	{
		if (string.IsNullOrEmpty(taskId))
		{
			return new List<PomoTask>();
		}
		List<PomoTask> pomoTasks = await (from pomoTask in App.Connection.Table<PomoTask>()
			where pomoTask.TaskId == taskId
			select pomoTask).ToListAsync();
		List<string> list = (from pomoTask in pomoTasks
			where !string.IsNullOrEmpty(pomoTask.PomoId)
			select pomoTask.PomoId).Distinct().ToList();
		if (!list.Any())
		{
			return new List<PomoTask>();
		}
		HashSet<string> currentUserPomoIds = new HashSet<string>((await GetPomoInIds(list)).Select((PomodoroModel pomo) => pomo.Id));
		return pomoTasks.Where((PomoTask pomoTask) => currentUserPomoIds.Contains(pomoTask.PomoId)).ToList();
	}

	public static async Task UpdatePomoTaskId(string oldTaskId, string newTaskId)
	{
		List<PomoTask> pomoTasks = await GetPomoTasksByTaskId(oldTaskId);
		if (pomoTasks == null || !pomoTasks.Any())
		{
			return;
		}
		foreach (PomoTask item in pomoTasks)
		{
			item.TaskId = newTaskId;
		}
		await UpdateAllPomoTask(pomoTasks);
		List<PomodoroModel> list = await GetPomoInIds(pomoTasks.Select((PomoTask p) => p.PomoId).Distinct().ToList());
		foreach (PomodoroModel item2 in list)
		{
			if (item2.SyncStatus == 2)
			{
				item2.SyncStatus = 1;
			}
		}
		await UpdateAllAsync(list);
	}
}
