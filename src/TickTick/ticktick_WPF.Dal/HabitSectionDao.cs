using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class HabitSectionDao
{
	public static async Task<long> GetMaxSortOrder()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return (await (from item in App.Connection.Table<HabitSectionModel>()
			where item.UserId == userId && item.SyncStatus != -1
			orderby item.SortOrder descending
			select item).FirstOrDefaultAsync())?.SortOrder ?? 0;
	}

	public static async Task<List<HabitSectionModel>> GetAllHabitSections()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitSectionModel>()
			where v.UserId == userId
			select v).ToListAsync();
	}

	public static async Task<List<HabitSectionModel>> GetNeedPostHabitSection()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitSectionModel>()
			where v.UserId == userId && (v.SyncStatus == 0 || v.SyncStatus == 1 || v.SyncStatus == -1)
			select v).ToListAsync();
	}

	public static async Task<HabitSectionModel> GetHabitSectionById(string sectionId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<HabitSectionModel>()
			where v.UserId == userId && v.Id == sectionId && v.SyncStatus != -1
			select v).FirstOrDefaultAsync();
	}

	public static async Task UpdateAsync(HabitSectionModel model)
	{
		await App.Connection.UpdateAsync(model);
	}

	public static async Task InsertAsync(HabitSectionModel model)
	{
		await App.Connection.InsertAsync(model);
	}

	public static async Task InsertAllAsync(List<HabitSectionModel> model)
	{
		await App.Connection.InsertAllAsync(model);
	}

	public static async Task DeleteAllAsync(List<string> modelIds)
	{
		if (modelIds != null && modelIds.Count != 0)
		{
			await Task.WhenAll(modelIds.Select((string modelId) => App.Connection.ExecuteAsync("DELETE FROM HabitSectionModel WHERE Id=?", modelId)));
		}
	}
}
