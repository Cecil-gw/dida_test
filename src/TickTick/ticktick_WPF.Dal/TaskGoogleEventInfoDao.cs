using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public class TaskGoogleEventInfoDao : BaseDao<TaskGoogleEventInfo>
{
	public static async Task<TaskGoogleEventInfo> GetByTaskIdAsync(string taskId)
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (string.IsNullOrEmpty(loginUserId) || string.IsNullOrEmpty(taskId))
		{
			return null;
		}
		TaskGoogleEventInfo taskGoogleEventInfo = (await App.Connection.QueryAsync<TaskGoogleEventInfo>("SELECT * FROM TaskGoogleEventInfo WHERE userId=? AND taskId=? AND deleted=0 ORDER BY etimestamp DESC LIMIT 1", new object[2] { loginUserId, taskId })).FirstOrDefault();
		taskGoogleEventInfo?.RestoreNestedData();
		return taskGoogleEventInfo;
	}

	public static async Task<bool> MergeSyncPageAsync(IEnumerable<TaskGoogleEventInfo> remoteItems, IEnumerable<string> deletedIds)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		if (string.IsNullOrEmpty(userId))
		{
			return false;
		}
		bool changed = false;
		List<string> list = deletedIds?.Where((string id) => !string.IsNullOrEmpty(id)).Distinct().ToList() ?? new List<string>();
		foreach (string item in list)
		{
			bool flag = changed;
			changed = flag | (await DeleteByServerIdAsync(item, userId) > 0);
		}
		List<TaskGoogleEventInfo> items = remoteItems?.Where((TaskGoogleEventInfo item) => item != null && !string.IsNullOrEmpty(item.id)).ToList() ?? new List<TaskGoogleEventInfo>();
		if (items.Count == 0)
		{
			return changed;
		}
		Dictionary<string, TaskGoogleEventInfo> localById = (from info in await (from info in App.Connection.Table<TaskGoogleEventInfo>()
				where info.userId == userId
				select info).ToListAsync()
			where !string.IsNullOrEmpty(info.id)
			group info by info.id).ToDictionary((IGrouping<string, TaskGoogleEventInfo> group) => group.Key, (IGrouping<string, TaskGoogleEventInfo> group) => group.OrderByDescending((TaskGoogleEventInfo info) => info._Id).First());
		List<TaskGoogleEventInfo> additions = new List<TaskGoogleEventInfo>();
		List<TaskGoogleEventInfo> updates = new List<TaskGoogleEventInfo>();
		foreach (TaskGoogleEventInfo item2 in items)
		{
			if (item2.deleted)
			{
				bool flag = changed;
				changed = flag | (await DeleteByServerIdAsync(item2.id, userId) > 0);
				continue;
			}
			item2.userId = userId;
			item2.PrepareForStorage();
			if (localById.TryGetValue(item2.id, out var value))
			{
				item2._Id = value._Id;
				updates.Add(item2);
			}
			else
			{
				additions.Add(item2);
			}
		}
		if (additions.Count > 0)
		{
			await App.Connection.InsertAllAsync(additions);
			changed = true;
		}
		if (updates.Count > 0)
		{
			await App.Connection.UpdateAllAsync(updates);
			changed = true;
		}
		return changed;
	}

	public static async Task<bool> DeleteConnectionsExceptAsync(IReadOnlyCollection<string> connectIds)
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (string.IsNullOrEmpty(loginUserId))
		{
			return false;
		}
		if (connectIds == null || connectIds.Count == 0)
		{
			return await DeleteAllForCurrentUserAsync();
		}
		string text = string.Join(",", connectIds.Select((string _) => "?"));
		List<object> list = new List<object> { loginUserId };
		list.AddRange(connectIds.Cast<object>());
		return await App.Connection.ExecuteAsync("DELETE FROM TaskGoogleEventInfo WHERE userId=? AND (connectId IS NULL OR connectId NOT IN (" + text + "))", list.ToArray()) > 0;
	}

	public static async Task<bool> DeleteByConnectionIdAsync(string connectId)
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (string.IsNullOrEmpty(loginUserId) || string.IsNullOrEmpty(connectId))
		{
			return false;
		}
		return await App.Connection.ExecuteAsync("DELETE FROM TaskGoogleEventInfo WHERE userId=? AND connectId=?", loginUserId, connectId) > 0;
	}

	public static async Task<bool> DeleteProjectsExceptAsync(string connectId, IReadOnlyCollection<string> projectIds)
	{
		if (projectIds == null || projectIds.Count == 0)
		{
			return await DeleteByConnectionIdAsync(connectId);
		}
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (string.IsNullOrEmpty(loginUserId) || string.IsNullOrEmpty(connectId))
		{
			return false;
		}
		string text = string.Join(",", projectIds.Select((string _) => "?"));
		List<object> list = new List<object> { loginUserId, connectId };
		list.AddRange(projectIds.Cast<object>());
		return await App.Connection.ExecuteAsync("DELETE FROM TaskGoogleEventInfo WHERE userId=? AND connectId=? AND (projectId IS NULL OR projectId NOT IN (" + text + "))", list.ToArray()) > 0;
	}

	public static async Task<bool> DeleteAllForCurrentUserAsync()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		if (string.IsNullOrEmpty(loginUserId))
		{
			return false;
		}
		return await App.Connection.ExecuteAsync("DELETE FROM TaskGoogleEventInfo WHERE userId=?", loginUserId) > 0;
	}

	private static Task<int> DeleteByServerIdAsync(string id, string userId)
	{
		return App.Connection.ExecuteAsync("DELETE FROM TaskGoogleEventInfo WHERE id=? AND userId=?", id, userId);
	}
}
