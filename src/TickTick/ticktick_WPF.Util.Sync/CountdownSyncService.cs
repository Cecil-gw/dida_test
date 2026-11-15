using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.Sync;

public static class CountdownSyncService
{
	private static readonly SemaphoreLocker SyncLock;

	private static DateTime _lastSyncTime;

	private static DateTime _lastLaunchSyncTime;

	static CountdownSyncService()
	{
		SyncLock = new SemaphoreLocker();
	}

	public static async Task SyncCountdowns(bool notify = true)
	{
		_lastLaunchSyncTime = DateTime.Now;
		SyncLock.LockAsync(async delegate
		{
			if (_lastSyncTime >= _lastLaunchSyncTime)
			{
				return;
			}
			_lastSyncTime = DateTime.Now;
			try
			{
				UtilLog.Info("sync countdowns: " + _lastSyncTime.ToString());
				await PullCountdowns(notify);
				await CommitCountdowns();
			}
			catch (Exception ex)
			{
				UtilLog.Warn("SyncCountdowns Failed: " + ExceptionUtils.BuildExceptionMessage(ex));
			}
			finally
			{
				Application.Current?.Dispatcher?.Invoke(DataChangedNotifier.NotifyCountdownsSyncDone);
			}
		});
	}

	private static async Task<CountdownCollection> PullCountdowns(bool notify = true)
	{
		if (LocalSettings.Settings.EnableCountDownDays && Utils.IsNetworkAvailable())
		{
			CountdownCollection countdowns = await CountdownService.PullRemoteCountdowns();
			if (countdowns != null && countdowns.Countdowns != null && await CountdownService.MergeRemoteCountdowns(countdowns.Countdowns) && notify)
			{
				Application.Current?.Dispatcher?.Invoke(delegate
				{
					DataChangedNotifier.NotifyCountdownsChanged();
				});
			}
			return countdowns;
		}
		return null;
	}

	private static async Task BatchUpdateCountdown()
	{
		List<CountdownModel> list = await CountdownDao.GetNeedPostCountdowns();
		SyncCountdownBean syncBean = new SyncCountdownBean();
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (CountdownModel item in list)
		{
			switch (item.SyncStatus)
			{
			case 0:
			case 4:
				syncBean.Add.Add(item);
				break;
			case 1:
				syncBean.Update.Add(item);
				break;
			case -1:
				syncBean.Delete.Add(item.Id);
				break;
			}
		}
		if (syncBean.Empty())
		{
			return;
		}
		UtilLog.Info($"BatchUpdateCountdown: Add={syncBean.Add.Count}, Update={syncBean.Update.Count}, Delete={syncBean.Delete.Count}");
		BatchUpdateResult result = await Communicator.UpdateCountdowns(syncBean);
		if (result != null)
		{
			if (result != null && result.Id2etag.Count > 0)
			{
				try
				{
					foreach (string key in result.Id2etag.Keys)
					{
						string etag = result.Id2etag[key];
						await CountdownDao.SaveCountdownEtag(key, etag);
					}
					foreach (string item2 in syncBean.Delete)
					{
						CountdownModel countdownModel = await CountdownDao.GetCountdownById(item2);
						if (countdownModel != null && countdownModel.Deleted == 1)
						{
							await App.Connection.DeleteAsync(countdownModel);
						}
					}
				}
				catch (Exception ex)
				{
					UtilLog.Warn("BatchUpdateCountdown Failed: " + ExceptionUtils.BuildExceptionMessage(ex));
				}
			}
		}
		if (result != null)
		{
			Dictionary<string, string> id2error = result.Id2error;
			if (id2error != null && id2error.Count > 0)
			{
				try
				{
					if (result.Id2error != null && result.Id2error.Any())
					{
						foreach (string key2 in result.Id2error.Keys)
						{
							switch (result.Id2error[key2])
							{
							case "EXISTED":
								await CountdownDao.SetSyncStatus(key2, 1);
								break;
							case "DELETED":
							{
								CountdownModel countdownModel2 = await CountdownDao.GetCountdownById(key2);
								if (countdownModel2 != null)
								{
									countdownModel2.Deleted = 1;
									countdownModel2.SyncStatus = 2;
									await CountdownDao.UpdateAsync(countdownModel2);
								}
								break;
							}
							case "EXCEED_QUOTA":
								await CountdownDao.SetSyncStatus(key2, 4);
								break;
							case "NOT_EXISTED":
								await CountdownDao.SetSyncStatus(key2, 0);
								break;
							}
						}
					}
				}
				catch (Exception ex2)
				{
					UtilLog.Warn("BatchUpdateCountdown Error Parse Failed: " + ExceptionUtils.BuildExceptionMessage(ex2));
				}
			}
		}
		if (syncBean.Delete.Count <= 0)
		{
			return;
		}
		foreach (string item3 in syncBean.Delete)
		{
			CountdownDao.SetSyncStatus(item3, 2);
		}
	}

	private static async Task CommitCountdowns(bool immediately = false)
	{
		if (Utils.IsNetworkAvailable())
		{
			await BatchUpdateCountdown();
		}
	}
}
