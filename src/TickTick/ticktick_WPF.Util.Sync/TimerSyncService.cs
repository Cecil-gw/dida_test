using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.Sync;

public class TimerSyncService
{
	private static readonly TTAsyncLocker TimerPullLocker = new TTAsyncLocker(1, 1);

	private static readonly HashSet<string> PulledKey = new HashSet<string>();

	public static async Task PullRemoteTimers()
	{
		await TimerPullLocker.RunAsync((Func<Task>)PullTimers);
	}

	private static async Task PullTimers()
	{
		if (await PullTimersCore(Communicator.PullRemoteTimers, MergeRemoteTimers))
		{
			PomoNotifier.NotifyTimerChanged();
		}
	}

	internal static async Task<bool> PullTimersCore(Func<Task<List<TimerModel>>> pull, Func<List<TimerModel>, Task<bool>> merge)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		List<TimerModel> remotes = await pull();
		if (remotes != null && isCurrentUser())
		{
			bool flag = await merge(remotes);
			UtilLog.Info($"PullRemoteTimers : Count {remotes.Count}");
			return isCurrentUser() && flag;
		}
		return false;
	}

	internal static async Task<bool> MergeRemoteTimers(List<TimerModel> remoteTimers)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		List<TimerModel> list = (await BaseDao<TimerModel>.GetAllAsync()) ?? new List<TimerModel>();
		if (!isCurrentUser())
		{
			return false;
		}
		foreach (TimerModel remoteTimer in remoteTimers)
		{
			LocalSettings.Settings.ObserveSyncedFocusTimer(remoteTimer);
		}
		List<TimerModel> added = new List<TimerModel>();
		List<TimerModel> updated = new List<TimerModel>();
		List<TimerModel> deleted = new List<TimerModel>();
		if (remoteTimers.Any())
		{
			foreach (TimerModel remote in remoteTimers)
			{
				TimerModel timerModel = list.FirstOrDefault((TimerModel timer) => timer.Id == remote.Id);
				if (timerModel != null)
				{
					if (timerModel.Etag != remote.Etag && timerModel.SyncStatus == 2)
					{
						remote._Id = timerModel._Id;
						remote.UserId = Utils.GetCurrentUserIdInt().ToString();
						remote.SyncStatus = 2;
						updated.Add(remote);
					}
					list.Remove(timerModel);
				}
				else
				{
					remote.UserId = Utils.GetCurrentUserIdInt().ToString();
					remote.SyncStatus = 2;
					added.Add(remote);
				}
			}
			if (list.Any())
			{
				deleted.AddRange(list.Where((TimerModel local) => local.SyncStatus == 2));
			}
			if (added.Any())
			{
				await App.Connection.InsertAllAsync(added);
			}
			if (updated.Any())
			{
				await App.Connection.UpdateAllAsync(updated);
			}
			if (deleted.Any())
			{
				foreach (TimerModel item in deleted)
				{
					await App.Connection.DeleteAsync(item);
				}
			}
		}
		else if (list.Any())
		{
			foreach (TimerModel item2 in list)
			{
				if (item2.SyncStatus != 0)
				{
					await App.Connection.DeleteAsync(item2);
				}
			}
			return true;
		}
		return added.Any() || updated.Any() || deleted.Any();
	}

	public static async Task PostTimers()
	{
		List<TimerModel> list = await TimerDao.GetNeedPostTimers();
		ModelSyncBean<TimerModel> syncBean = new ModelSyncBean<TimerModel>();
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (TimerModel item in list)
		{
			switch (item.SyncStatus)
			{
			case 0:
				syncBean.Add.Add(item);
				break;
			case 1:
				syncBean.Update.Add(item);
				break;
			case -1:
				syncBean.Deleted.Add(item.Id);
				break;
			}
		}
		if (syncBean.Empty())
		{
			return;
		}
		BatchUpdateResult result = await Communicator.UpdateTimers(syncBean);
		if (result != null && result.Id2etag.Count > 0)
		{
			try
			{
				foreach (string key in result.Id2etag.Keys)
				{
					string etag = result.Id2etag[key];
					await TimerService.SaveEtag(key, etag);
				}
			}
			catch (Exception)
			{
			}
		}
		if (result != null && result.Id2error.Count > 0)
		{
			try
			{
				foreach (string key2 in result.Id2error.Keys)
				{
					switch (result.Id2error[key2])
					{
					case "OBJ_NOT_EXISTED":
						TimerDao.DeleteByIds(new List<string> { key2 });
						PomoNotifier.NotifyTimerChanged();
						break;
					case "OBJ_BOUND":
						PomoService.RemovePomoTaskTimerId(key2);
						TimerDao.DeleteByIds(new List<string> { key2 });
						PomoNotifier.NotifyTimerChanged();
						break;
					case "EXISTED":
						TimerDao.SetTimerSyncStatus(key2, 1);
						break;
					case "NOT_EXISTED":
						TimerDao.SetTimerSyncStatus(key2, 0);
						break;
					}
				}
			}
			catch (Exception)
			{
			}
		}
		if (result != null)
		{
			TimerDao.DeleteByIds(syncBean.Deleted);
		}
	}

	public static async Task<TimerOverviewModel> PullTimerOverview(string timerId)
	{
		TimerModel timer = await TimerDao.GetTimerById(timerId);
		if (timer == null)
		{
			return null;
		}
		TimerOverviewModel overview = await Communicator.PullTimerOverview(timerId);
		if (overview == null && timer.Overview == null)
		{
			Dictionary<DateTime, double> dictionary = await TimerService.GetTimerRecord(timerId, timer.ObjId, timer.ObjType);
			overview = new TimerOverviewModel
			{
				Days = dictionary.Keys.Count,
				Today = (dictionary.ContainsKey(DateTime.Today) ? ((long)dictionary[DateTime.Today]) : 0),
				Total = (long)dictionary.Values.ToList().Sum()
			};
		}
		if (overview != null)
		{
			overview.Date = DateUtils.GetDateNum(DateTime.Today);
			await TimerDao.SaveOverview(timerId, JsonConvert.SerializeObject(overview));
		}
		return overview;
	}

	public static async Task<bool> PullTimerStatistics(string timerId, DateTime start, DateTime end, string interval)
	{
		TimerModel timer = await TimerDao.GetTimerById(timerId);
		if (timer == null)
		{
			return false;
		}
		string key = TimerService.GetTimerStatisticsKey(timerId, start, end, interval);
		string statisticsString = await Communicator.PullTimerStatistics(timerId, start, end, interval);
		TimerStatisticsModel timerStatisticsModel = await TimerDao.GetStatistics(key);
		Dictionary<string, long> dictionary = null;
		try
		{
			dictionary = JsonConvert.DeserializeObject<Dictionary<string, long>>(statisticsString);
		}
		catch (Exception)
		{
		}
		if (dictionary == null)
		{
			if (timerStatisticsModel == null)
			{
				await TimerDao.SaveStatistics(key, JsonConvert.SerializeObject(await TimerService.CalTimerStatistics(timer, start, end, interval)));
				return true;
			}
		}
		else if (timerStatisticsModel == null || timerStatisticsModel.Value != statisticsString)
		{
			await TimerDao.SaveStatistics(key, statisticsString);
			return true;
		}
		return false;
	}

	public static async Task<bool> TryPullTimerStatistics(string timerId, DateTime start, DateTime end, string interval)
	{
		string timerStatisticsKey = TimerService.GetTimerStatisticsKey(timerId, start, end, interval);
		if (PulledKey.Contains(timerStatisticsKey))
		{
			return false;
		}
		PulledKey.Add(timerStatisticsKey);
		return await PullTimerStatistics(timerId, start, end, interval);
	}

	public static void ClearPulledKey()
	{
		PulledKey.Clear();
	}
}
