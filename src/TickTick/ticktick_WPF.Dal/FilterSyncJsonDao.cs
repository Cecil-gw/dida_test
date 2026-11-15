using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class FilterSyncJsonDao
{
	public static async Task TrySaveFilter(string id)
	{
		FilterModel localFilter = await FilterDao.GetFilterById(id);
		if (localFilter != null && await GetSavedJson(id) == null)
		{
			FilterSyncedJsonModel item = new FilterSyncedJsonModel
			{
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				JsonString = JsonConvert.SerializeObject(localFilter),
				FilterId = id
			};
			await App.Connection.InsertAsync(item);
		}
	}

	public static async Task<FilterSyncedJsonModel> GetSavedJson(string filterId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<FilterSyncedJsonModel>()
			where model.UserId == userId && model.FilterId == filterId
			select model).FirstOrDefaultAsync();
	}

	public static async Task BatchDeleteFilters(List<string> filterIds)
	{
		if (filterIds == null || !filterIds.Any())
		{
			return;
		}
		foreach (string filterId in filterIds)
		{
			FilterSyncedJsonModel filterSyncedJsonModel = await GetSavedJson(filterId);
			if (filterSyncedJsonModel != null)
			{
				await App.Connection.DeleteAsync(filterSyncedJsonModel);
			}
		}
	}
}
