using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public class EventBatchHandler : BatchHandler
{
	public EventBatchHandler(string userId, SyncResult syncResult)
		: base(userId, syncResult)
	{
	}

	public static async Task<List<SyncStatusModel>> GetLocalModifiedEvents()
	{
		return await TaskDao.GetNeedPostTasks(9);
	}

	public static async Task<List<SyncStatusModel>> GetLocalAddedEvents()
	{
		return await TaskDao.GetNeedPostTasks(10);
	}

	public static async Task<List<SyncStatusModel>> GetLocalDeletedEvents()
	{
		return await TaskDao.GetNeedPostTasks(11);
	}

	public static async Task<List<SyncStatusModel>> GetLocalMoveEvents()
	{
		return await TaskDao.GetNeedPostTasks(12);
	}

	public static async Task HandleCommitResult(List<EventUpdateResult> results, SyncEventBean bean, bool checkEtag = true)
	{
		if (results == null || !results.Any())
		{
			return;
		}
		foreach (EventUpdateResult result in results)
		{
			await HandleCommitResult(result, bean, checkEtag);
		}
	}

	private static async Task HandleCommitResult(EventUpdateResult result, SyncEventBean bean, bool checkEtag)
	{
		if (result != null && result.Id2error.Any())
		{
			foreach (KeyValuePair<string, Dictionary<string, string>> id in result.Id2error)
			{
				if (!id.Value.Any())
				{
					continue;
				}
				foreach (KeyValuePair<string, string> key in id.Value)
				{
					if (key.Value.ToLower() == "existed")
					{
						CalendarEventDao.ProtectFromRemoteSnapshotDelete(await CalendarEventDao.GetEventById(key.Key + "@" + id.Key));
						await SyncStatusDao.DeleteSyncStatus(key.Key, 10);
					}
				}
			}
		}
		if (!checkEtag)
		{
			foreach (CalendarAccountBean calendarAccountBean in bean.CalendarAccountBeans)
			{
				foreach (CalendarEventBean calendarBean in calendarAccountBean.CalendarBeans)
				{
					foreach (CalendarEventModel calEvent in calendarBean.Add)
					{
						CalendarEventDao.ProtectFromRemoteSnapshotDelete(await CalendarEventDao.GetEventById(calEvent.EventId + "@" + calendarBean.CalendarId));
						await SyncStatusDao.DeleteSyncStatus(calEvent.EventId, 10);
					}
					foreach (CalendarEventModel item in calendarBean.Update)
					{
						await SyncStatusDao.DeleteSyncStatus(item.EventId, 9);
					}
					foreach (EventMoveModel item2 in calendarBean.Move)
					{
						await SyncStatusDao.DeleteSyncStatus(item2.EventId, 12);
					}
					foreach (string item3 in calendarBean.Delete)
					{
						await SyncStatusDao.DeleteSyncStatus(item3, 11);
					}
				}
			}
		}
		List<string> list = await SyncStatusDao.GetEntityIdsByType(10);
		if (list != null && list.Any())
		{
			foreach (string eventId in list)
			{
				CalendarEventModel calendarEventModel = await UpdateEtag(result, eventId);
				if (calendarEventModel != null)
				{
					CalendarEventDao.ProtectFromRemoteSnapshotDelete(calendarEventModel);
					await SyncStatusDao.DeleteSyncStatus(eventId, 10);
				}
			}
		}
		List<string> list2 = await SyncStatusDao.GetEntityIdsByType(9);
		if (list2 != null && list2.Any())
		{
			foreach (string eventId in list2)
			{
				if (await UpdateEtag(result, eventId) != null)
				{
					await SyncStatusDao.DeleteSyncStatus(eventId, 9);
				}
			}
		}
		List<string> list3 = await SyncStatusDao.GetEntityIdsByType(12);
		if (list3 != null && list3.Any())
		{
			foreach (string eventId in list3)
			{
				if (await UpdateEtag(result, eventId) != null || !checkEtag)
				{
					await SyncStatusDao.DeleteSyncStatus(eventId, 12);
				}
			}
		}
		List<string> list4 = await SyncStatusDao.GetEntityIdsByType(11);
		if (list4 == null || !list4.Any())
		{
			return;
		}
		foreach (string eventId in list4)
		{
			await UpdateEtag(result, eventId);
			await SyncStatusDao.DeleteSyncStatus(eventId, 11);
		}
	}

	private static async Task<CalendarEventModel> UpdateEtag(EventUpdateResult result, string eventId)
	{
		if (!string.IsNullOrWhiteSpace(eventId))
		{
			CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventByEventId(eventId);
			if (calendarEventModel == null)
			{
				calendarEventModel = await CalendarEventDao.GetEventById(eventId);
			}
			CalendarEventModel calEvent = calendarEventModel;
			if (calEvent != null && !string.IsNullOrEmpty(calEvent.CalendarId))
			{
				foreach (Dictionary<string, string> value in result.Id2etag.Values)
				{
					if (value.Keys.Contains(calEvent.EventId))
					{
						calEvent.Etag = value[calEvent.EventId];
						await App.Connection.UpdateAsync(calEvent);
						return calEvent;
					}
				}
			}
		}
		return null;
	}
}
