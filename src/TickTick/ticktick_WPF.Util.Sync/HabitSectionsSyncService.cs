using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;

namespace ticktick_WPF.Util.Sync;

internal class HabitSectionsSyncService
{
	private static readonly SemaphoreLocker SyncLock = new SemaphoreLocker();

	private static bool _isWaitingUpdate = false;

	public static void SyncSections()
	{
		SyncLock.LockAsync(async delegate
		{
			PullHabitSections();
		});
	}

	public static async Task PullHabitSections()
	{
		if (LocalSettings.Settings.ShowHabit)
		{
			bool changed = await HabitService.PullAndMergeRemoteHabitSections();
			await BatchUpdateHabitSections();
			_ = changed;
			await HabitSectionCache.SetSections();
		}
	}

	public static async Task BatchUpdateHabitSections()
	{
		if (_isWaitingUpdate)
		{
			return;
		}
		_isWaitingUpdate = true;
		await Task.Delay(500);
		List<HabitSectionModel> list = await HabitSectionDao.GetNeedPostHabitSection();
		_isWaitingUpdate = false;
		if (!list.Any())
		{
			return;
		}
		SyncHabitSectionsBean bean = new SyncHabitSectionsBean();
		foreach (HabitSectionModel item in list)
		{
			switch (item.SyncStatus)
			{
			case 0:
				bean.Add.Add(item);
				break;
			case 1:
				bean.Update.Add(item);
				break;
			case -1:
				bean.Delete.Add(item.Id);
				break;
			}
		}
		BatchUpdateResult model = await Communicator.UpdateHabitSections(bean);
		if (model == null)
		{
			return;
		}
		try
		{
			foreach (string key in model.Id2etag.Keys)
			{
				string etag = model.Id2etag[key];
				await HabitService.SaveHabitSectionEtag(key, etag);
			}
			await HabitSectionDao.DeleteAllAsync(bean.Delete?.ToList());
		}
		catch (Exception)
		{
		}
	}
}
