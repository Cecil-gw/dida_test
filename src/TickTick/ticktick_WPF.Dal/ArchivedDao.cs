using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Dal;

public class ArchivedDao : BaseDao<ArchivedEventModel>
{
	public static async Task AddArchivedModel(string key, ArchiveKind kind, DateTime? start = null)
	{
		ArchivedEventModel item = new ArchivedEventModel
		{
			UserId = LocalSettings.Settings.LoginUserId,
			Key = key,
			StartTime = start,
			Kind = (int)kind
		};
		await App.Connection.InsertAsync(item);
	}

	public static async Task<bool> ExistArchivedModel(string key)
	{
		return await (from cal in App.Connection.Table<ArchivedEventModel>()
			where cal.Key == key && cal.UserId == LocalSettings.Settings.LoginUserId
			select cal).CountAsync() > 0;
	}

	public static async Task RemoveArchivedModel(string key)
	{
		List<ArchivedEventModel> list = await (from cal in App.Connection.Table<ArchivedEventModel>()
			where cal.Key == key
			select cal).ToListAsync();
		if (!list.Any())
		{
			return;
		}
		foreach (ArchivedEventModel item in list)
		{
			item.SyncStatus = -1;
		}
		await App.Connection.UpdateAllAsync(list);
	}

	public static string GenerateKey(string id, DateTime? startDate)
	{
		id = GetOriginalId(id);
		return id + "_time_" + (startDate?.Ticks ?? 0);
	}

	public static async Task<List<string>> GetArchivedKeys(ArchiveKind kind = ArchiveKind.Event)
	{
		string sql = $"select key from ArchivedEventModel where kind = {(int)kind} and SyncStatus != -1 and userid = '{LocalSettings.Settings.LoginUserId}'";
		return (await App.Connection.QueryAsync<EventKeyModel>(sql, Array.Empty<object>())).Select((EventKeyModel model) => model.Key).ToList();
	}

	public static async Task<List<ArchivedEventModel>> GetArchivedModels(ArchiveKind kind, bool needSync = false)
	{
		string text = $"select * from ArchivedEventModel where Kind = {(int)kind} and Userid = '{LocalSettings.Settings.LoginUserId}'";
		if (needSync)
		{
			text += $" and SyncStatus != {2}";
		}
		return await App.Connection.QueryAsync<ArchivedEventModel>(text, Array.Empty<object>());
	}

	public static string GetOriginalId(string id)
	{
		if (!string.IsNullOrEmpty(id) && id.Contains("_time_"))
		{
			int length = id.IndexOf("_time_", StringComparison.Ordinal);
			return id.Substring(0, length);
		}
		return id;
	}
}
