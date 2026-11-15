using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Sync.ClosedLoader;

public static class CompletionLoadStatusDao
{
	private static ClosedLoadStatus _allStatus;

	public static async Task SetStatusPulledDateByEntityId(string entityId, DateTime time)
	{
		ClosedLoadStatus closedLoadStatus = await GetLoadStatusByEntityId(entityId);
		if (closedLoadStatus.IsAllLoaded || closedLoadStatus.EarliestPulledDate < time.AddDays(1.0))
		{
			closedLoadStatus.IsAllLoaded = false;
			closedLoadStatus.EarliestPulledDate = time.AddDays(1.0);
		}
		await SaveCompletionLoadStatus(closedLoadStatus);
	}

	public static async Task<ClosedLoadStatus> GetLoadStatusByEntityId(string entityId)
	{
		if (entityId == "all")
		{
			return await GetStatusInAllList();
		}
		return await GetStatusByEntityId(entityId);
	}

	private static async Task<ClosedLoadStatus> GetStatusByEntityId(string entityId)
	{
		List<ClosedLoadStatus> status = await (from closedLoadStatus in App.Connection.Table<ClosedLoadStatus>()
			where closedLoadStatus.UserId == LocalSettings.Settings.LoginUserId && closedLoadStatus.EntityId == entityId
			select closedLoadStatus).ToListAsync();
		if (status == null || status.Count == 0)
		{
			ClosedLoadStatus s = new ClosedLoadStatus
			{
				EntityId = entityId?.ToLower(),
				EarliestPulledDate = DateTime.UtcNow,
				LastPullTime = default(DateTime),
				IsAllLoaded = false,
				IsNew = true,
				UserId = LocalSettings.Settings.LoginUserId
			};
			await App.Connection.InsertAsync(s);
			s.EarliestPulledDate = DateUtils.GetLocalTime(s.EarliestPulledDate);
			return s;
		}
		ClosedLoadStatus st = status[0];
		st.EarliestPulledDate = DateUtils.GetLocalTime(st.EarliestPulledDate);
		if (status.Count > 1)
		{
			for (int i = 1; i < status.Count; i++)
			{
				await App.Connection.DeleteAsync(status[i]);
			}
		}
		return st;
	}

	public static async Task SaveCompletionLoadStatus(ClosedLoadStatus status)
	{
		ClosedLoadStatus persistedStatus = CreatePersistenceCopy(status);
		if (status.IsNew)
		{
			ClosedLoadStatus closedLoadStatus = await (from s in App.Connection.Table<ClosedLoadStatus>()
				where s.UserId == LocalSettings.Settings.LoginUserId && s.EntityId == status.EntityId
				select s).FirstOrDefaultAsync();
			if (closedLoadStatus == null)
			{
				await App.Connection.InsertAsync(persistedStatus);
				status._Id = persistedStatus._Id;
			}
			else
			{
				status._Id = closedLoadStatus._Id;
				persistedStatus._Id = closedLoadStatus._Id;
				await App.Connection.UpdateAsync(persistedStatus);
			}
		}
		else
		{
			await App.Connection.UpdateAsync(persistedStatus);
		}
	}

	internal static ClosedLoadStatus CreatePersistenceCopy(ClosedLoadStatus status)
	{
		return new ClosedLoadStatus
		{
			_Id = status._Id,
			UserId = status.UserId,
			EntityId = status.EntityId,
			TagToken = status.TagToken,
			EarliestPulledDate = status.EarliestPulledDate.ToUniversalTime(),
			LastPullTime = status.LastPullTime,
			IsAllLoaded = status.IsAllLoaded,
			IsNew = status.IsNew
		};
	}

	public static async Task<ClosedLoadStatus> GetStatusInAllList()
	{
		if (_allStatus == null || _allStatus.IsNew || _allStatus.UserId != LocalSettings.Settings.LoginUserId)
		{
			_allStatus = await GetStatusByEntityId("all");
			if (_allStatus.IsAllLoaded)
			{
				_allStatus.IsAllLoaded = false;
			}
			if (Utils.IsEmptyDate(_allStatus.EarliestPulledDate))
			{
				_allStatus.EarliestPulledDate = DateTime.Now;
			}
		}
		return _allStatus;
	}

	public static async Task DeleteStatusByEntityId(string entityId)
	{
		List<ClosedLoadStatus> list = await (from s in App.Connection.Table<ClosedLoadStatus>()
			where s.UserId == LocalSettings.Settings.LoginUserId && s.EntityId == entityId
			select s).ToListAsync();
		if (list == null)
		{
			return;
		}
		foreach (ClosedLoadStatus item in list)
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task ResetPullTime(DateTime time)
	{
		List<ClosedLoadStatus> list = await (from s in App.Connection.Table<ClosedLoadStatus>()
			where s.UserId == LocalSettings.Settings.LoginUserId
			select s).ToListAsync();
		foreach (ClosedLoadStatus item in list)
		{
			if (item.EarliestPulledDate < time.AddDays(7.0) || !string.IsNullOrEmpty(item.TagToken))
			{
				item.EarliestPulledDate = time.AddDays(7.0);
				item.IsAllLoaded = false;
				item.TagToken = null;
			}
		}
		await App.Connection.UpdateAllAsync(list);
	}

	public static async Task SetTagTasksLoaded(string tag)
	{
		ClosedLoadStatus item = new ClosedLoadStatus
		{
			EntityId = tag?.ToLower(),
			EarliestPulledDate = default(DateTime),
			LastPullTime = default(DateTime),
			IsAllLoaded = true,
			IsNew = false,
			UserId = LocalSettings.Settings.LoginUserId
		};
		await App.Connection.InsertAsync(item);
	}

	public static async Task OnTagRename(string origin, string tagName)
	{
		List<ClosedLoadStatus> statuses = await (from s in App.Connection.Table<ClosedLoadStatus>()
			where s.UserId == LocalSettings.Settings.LoginUserId && s.EntityId == origin
			select s).ToListAsync();
		if (statuses == null)
		{
			return;
		}
		for (int index = 0; index < statuses.Count; index++)
		{
			ClosedLoadStatus closedLoadStatus = statuses[index];
			if (index == 0)
			{
				closedLoadStatus.EntityId = tagName;
				await App.Connection.UpdateAsync(closedLoadStatus);
			}
			else
			{
				await App.Connection.DeleteAsync(closedLoadStatus);
			}
		}
	}
}
