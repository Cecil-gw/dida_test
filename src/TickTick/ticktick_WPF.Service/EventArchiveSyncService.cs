using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TickTickDao;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public class EventArchiveSyncService
{
	private static readonly SemaphoreSlim ArchiveSyncLocker = new SemaphoreSlim(1, 1);

	public static void PullArchivedModels()
	{
		Task.Run((Func<Task>)PullArchivedModelsAsync).ContinueWith(UtilRun.LogTask);
	}

	public static async Task PullArchivedModelsAsync()
	{
		await ArchiveSyncLocker.WaitAsync();
		try
		{
			List<EventArchiveSyncModel> archiveModels = await Communicator.GetArchivedEvents();
			if (archiveModels == null)
			{
				return;
			}
			List<ArchivedEventModel> locals = await ArchivedDao.GetArchivedModels(ArchiveKind.Event);
			Dictionary<string, ArchivedEventModel> dictionary = await BuildLocalDictionary(locals);
			bool changed = dictionary.Count != locals.Count;
			List<ArchivedEventModel> added = new List<ArchivedEventModel>();
			foreach (EventArchiveSyncModel item in archiveModels)
			{
				if (!string.IsNullOrEmpty(item.eventId) && !dictionary.Remove(item.eventId))
				{
					added.Add(new ArchivedEventModel
					{
						UserId = LocalSettings.Settings.LoginUserId,
						Key = item.eventId,
						Kind = 0,
						SyncStatus = 2,
						StartTime = item.dueStart
					});
				}
			}
			foreach (ArchivedEventModel value in dictionary.Values)
			{
				if (value.SyncStatus == 2 || value.SyncStatus == -1)
				{
					await BaseDao<ArchivedEventModel>.DeleteAsync(value);
					changed = true;
				}
			}
			await BaseDao<ArchivedEventModel>.InsertAllAsync(added);
			if (changed || added.Count > 0)
			{
				DataChangedNotifier.NotifyCalendarChanged(200);
			}
		}
		finally
		{
			ArchiveSyncLocker.Release();
		}
	}

	public static async Task PushLocalArchivedModels()
	{
		await ArchiveSyncLocker.WaitAsync();
		try
		{
			List<ArchivedEventModel> pendingLocals = (await BuildLocalDictionary(await ArchivedDao.GetArchivedModels(ArchiveKind.Event))).Values.Where((ArchivedEventModel local) => local.SyncStatus != 2).ToList();
			EventArchivePostModel eventArchivePostModel = new EventArchivePostModel();
			foreach (ArchivedEventModel item in pendingLocals)
			{
				switch (item.SyncStatus)
				{
				case 0:
					eventArchivePostModel.add.Add(new EventArchiveSyncModel
					{
						eventId = item.Key,
						dueStart = item.StartTime,
						title = item.Title
					});
					break;
				case -1:
					eventArchivePostModel.delete.Add(item.Key);
					break;
				}
			}
			bool flag = eventArchivePostModel.Empty;
			if (!flag)
			{
				flag = !(await Communicator.PostArchivedEvents(eventArchivePostModel));
			}
			if (flag)
			{
				return;
			}
			foreach (ArchivedEventModel item2 in pendingLocals)
			{
				switch (item2.SyncStatus)
				{
				case 0:
					await App.Connection.ExecuteAsync("UPDATE ArchivedEventModel SET SyncStatus=? WHERE _Id=? AND SyncStatus=?", 2, item2._Id, 0);
					break;
				case -1:
					await App.Connection.ExecuteAsync("DELETE FROM ArchivedEventModel WHERE _Id=? AND SyncStatus=?", item2._Id, -1);
					break;
				}
			}
		}
		finally
		{
			ArchiveSyncLocker.Release();
		}
	}

	private static async Task<Dictionary<string, ArchivedEventModel>> BuildLocalDictionary(IEnumerable<ArchivedEventModel> locals)
	{
		Dictionary<string, ArchivedEventModel> localDict = new Dictionary<string, ArchivedEventModel>(StringComparer.Ordinal);
		foreach (ArchivedEventModel local in locals)
		{
			ArchivedEventModel value;
			if (string.IsNullOrEmpty(local.Key))
			{
				await BaseDao<ArchivedEventModel>.DeleteAsync(local);
			}
			else if (!localDict.TryGetValue(local.Key, out value))
			{
				localDict[local.Key] = local;
			}
			else if (local._Id > value._Id)
			{
				await BaseDao<ArchivedEventModel>.DeleteAsync(value);
				localDict[local.Key] = local;
			}
			else
			{
				await BaseDao<ArchivedEventModel>.DeleteAsync(local);
			}
		}
		return localDict;
	}
}
