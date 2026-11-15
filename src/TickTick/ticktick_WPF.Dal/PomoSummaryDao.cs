using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public class PomoSummaryDao : BaseDao<PomodoroSummaryModel>
{
	public static async Task DeletePomosByTaskId(string taskId)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<PomodoroSummaryModel> list = await (from v in App.Connection.Table<PomodoroSummaryModel>()
			where v.taskId == taskId && v.userId == userId
			select v).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			return;
		}
		foreach (PomodoroSummaryModel item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task CleanPomosByTaskId(string taskId)
	{
		UtilLog.Info("CleanPomos taskId: " + taskId);
		string userId = LocalSettings.Settings.LoginUserId;
		List<PomodoroSummaryModel> list = await (from v in App.Connection.Table<PomodoroSummaryModel>()
			where v.taskId == taskId && v.userId == userId
			select v).ToListAsync();
		if (list == null || list.Count <= 0)
		{
			UtilLog.Info("CleanPomos pomos: null");
			return;
		}
		list.ForEach(delegate(PomodoroSummaryModel pomo)
		{
			pomo.count = 0;
			pomo.duration = 0L;
			pomo.PomoDuration = 0L;
			pomo.StopwatchDuration = 0L;
			pomo.focuses = null;
		});
		UtilLog.Info("CleanPomos pomos: " + JsonConvert.SerializeObject(list));
		await App.Connection.UpdateAllAsync(list);
	}

	public static async Task SavePomoSummaries(List<PomodoroSummaryModel> pomos)
	{
		await App.Connection.InsertAllAsync(pomos);
	}

	public static async Task SavePomoSummary(PomodoroSummaryModel pomo)
	{
		List<PomodoroSummaryModel> list = await (from p in App.Connection.Table<PomodoroSummaryModel>()
			where p.id == pomo.id
			select p).ToListAsync();
		if (list != null && list.Count > 0 && !string.IsNullOrEmpty(pomo.id))
		{
			foreach (PomodoroSummaryModel item in list)
			{
				item.count = pomo.count;
				item.PomoDuration = pomo.PomoDuration;
				item.estimatedPomo = pomo.estimatedPomo;
				item.EstimatedDuration = pomo.EstimatedDuration;
				item.StopwatchDuration = pomo.StopwatchDuration;
				item.taskId = pomo.taskId;
				item.userId = pomo.userId;
				item.focuses = pomo.focuses;
			}
			await App.Connection.UpdateAllAsync(list);
		}
		else
		{
			if (string.IsNullOrEmpty(pomo.id))
			{
				pomo.id = Utils.GetGuid();
			}
			await App.Connection.InsertAsync(pomo);
		}
	}

	public static async Task<List<PomodoroSummaryModel>> GetPomosByTaskId(string taskId)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from v in App.Connection.Table<PomodoroSummaryModel>()
			where v.taskId == taskId && v.userId == userId
			select v).ToListAsync();
	}

	public static async Task<PomodoroSummaryModel> GetPomoByTaskId(string taskId)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		List<PomodoroSummaryModel> list = await (from v in App.Connection.Table<PomodoroSummaryModel>()
			where v.taskId == taskId && v.userId == userId
			select v).ToListAsync();
		return (list == null || list.Count != 1) ? null : list[0];
	}

	public static async Task<List<PomodoroSummaryModel>> GetPomosByTaskIds(List<string> taskIds)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from v in App.Connection.Table<PomodoroSummaryModel>()
			where taskIds.Contains(v.taskId) && v.userId == userId
			select v).ToListAsync();
	}
}
