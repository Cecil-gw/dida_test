using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Dal;

public class FilterDao : BaseDao<FilterModel>
{
	public static async Task AddFilter(FilterModel filter)
	{
		if (await GetFilterById(filter.id) == null)
		{
			filter.name = filter.name.Trim();
			if (string.IsNullOrEmpty(filter.userId))
			{
				filter.userId = Utils.GetCurrentUserIdInt().ToString();
			}
			await App.Connection.InsertAsync(filter);
			CacheManager.UpdateFilter(filter);
		}
	}

	private static async Task BatchInsertFilter(IReadOnlyCollection<FilterModel> filters)
	{
		if (filters == null || filters.Count <= 0)
		{
			return;
		}
		List<FilterModel> newFilters = new List<FilterModel>();
		foreach (FilterModel filter in filters)
		{
			if (await GetFilterById(filter.id) == null)
			{
				if (string.IsNullOrEmpty(filter.userId))
				{
					filter.userId = Utils.GetCurrentUserIdInt().ToString();
				}
				filter.name = filter.name.Trim();
				if (!newFilters.Select((FilterModel f) => f.id).ToList().Contains(filter.id))
				{
					newFilters.Add(filter);
				}
			}
		}
		if (newFilters.Count <= 0)
		{
			return;
		}
		await App.Connection.InsertAllAsync(newFilters);
		foreach (FilterModel item in newFilters)
		{
			CacheManager.UpdateFilter(item);
		}
	}

	public static async Task UpdateFilter(FilterModel filter, bool needSync = true)
	{
		filter.name = filter.name.Trim();
		if (needSync)
		{
			filter.syncStatus = ((filter.syncStatus != 0) ? 1 : 0);
			filter.modifiedTime = DateTime.Now;
		}
		if (string.IsNullOrEmpty(filter.userId))
		{
			filter.userId = Utils.GetCurrentUserIdInt().ToString();
		}
		CacheManager.UpdateFilter(filter);
		await FilterSyncJsonDao.TrySaveFilter(filter.id);
		await App.Connection.UpdateAsync(filter);
	}

	private static async Task BatchUpdateFilter(List<FilterModel> filters)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		foreach (FilterModel filter in filters)
		{
			filter.name = filter.name.Trim();
			if (string.IsNullOrEmpty(filter.userId))
			{
				filter.userId = userId;
			}
		}
		await App.Connection.UpdateAllAsync(filters);
		foreach (FilterModel filter2 in filters)
		{
			CacheManager.UpdateFilter(filter2);
		}
	}

	public static async Task<Dictionary<string, FilterModel>> GetLocalSyncedFilterMap()
	{
		List<FilterModel> obj = await GetAllFiltersWithDeleted();
		Dictionary<string, FilterModel> dictionary = new Dictionary<string, FilterModel>();
		foreach (FilterModel item in obj)
		{
			if (!dictionary.ContainsKey(item.id))
			{
				dictionary.Add(item.id, item);
			}
		}
		return dictionary;
	}

	private static async Task<List<FilterModel>> GetAllFiltersWithDeleted()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from x in App.Connection.Table<FilterModel>()
			where x.userId == userId
			orderby x.sortOrder
			select x).ToListAsync();
	}

	public static async Task<List<FilterModel>> GetAllFilters()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from x in App.Connection.Table<FilterModel>()
			where x.deleted != 1 && x.userId == userId
			orderby x.sortOrder
			select x).ToListAsync();
	}

	public static async Task SaveServerMergeData(List<FilterModel> added, List<FilterModel> updated, List<string> deleted)
	{
		if (added != null && added.Count > 0)
		{
			await BatchInsertFilter(added);
		}
		if (updated != null && updated.Count > 0)
		{
			await BatchUpdateFilter(updated);
			await FilterSyncJsonDao.BatchDeleteFilters(updated.Select((FilterModel filter) => filter.id).ToList());
		}
		if (deleted.Count > 0)
		{
			await BatchDeleteFilter(deleted);
		}
	}

	public static async Task<long> GetNextFilterSortOrder()
	{
		List<FilterModel> list = await (from f in App.Connection.Table<FilterModel>()
			where f.userId == LocalSettings.Settings.LoginUserId
			orderby f.sortOrder
			select f).ToListAsync();
		FilterModel filterModel = list.FirstOrDefault();
		if (filterModel != null)
		{
			long num = filterModel.sortOrder - 268435456;
			if (num > filterModel.sortOrder)
			{
				num = 268435456L;
				foreach (FilterModel item in list)
				{
					item.sortOrder = num;
					num += 268435456;
					UpdateFilter(item);
				}
				num = -268435456L;
			}
			return num;
		}
		return 0L;
	}

	private static async Task BatchDeleteFilter(IEnumerable<string> deleted)
	{
		foreach (string id in deleted)
		{
			await DeleteFilter(id);
			CacheManager.DeleteFilter(id);
		}
	}

	public static async Task<List<FilterModel>> GetNeedPostFilter()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from x in App.Connection.Table<FilterModel>()
			where x.syncStatus != 2 && x.userId == userId
			orderby x.sortOrder
			select x).ToListAsync();
	}

	public static async Task SaveCommitResultBackToDb(Dictionary<string, string> id2Etag, Collection<string> deleted)
	{
		if (id2Etag != null)
		{
			foreach (string serverId in id2Etag.Keys)
			{
				FilterModel filter = await GetFilterById(serverId);
				if (filter != null)
				{
					filter.syncStatus = 2;
					filter.etag = id2Etag[serverId];
					await App.Connection.UpdateAsync(filter);
					CacheManager.UpdateFilter(filter);
				}
			}
		}
		if (deleted == null || deleted.Count <= 0)
		{
			return;
		}
		foreach (string item in deleted)
		{
			FilterModel filter = await GetFilterById(item);
			if (filter != null)
			{
				filter.deleted = 1;
				filter.syncStatus = 2;
				await App.Connection.UpdateAsync(filter);
				CacheManager.UpdateFilter(filter);
			}
		}
	}

	private static async Task DeleteFilter(string id)
	{
		List<FilterModel> list = await GetFiltersById(id);
		if (list == null || list.Count < 0)
		{
			return;
		}
		foreach (FilterModel item in list)
		{
			item.deleted = 1;
			item.syncStatus = 1;
			await UpdateFilter(item);
		}
	}

	private static async Task<List<FilterModel>> GetFiltersById(string id)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from v in App.Connection.Table<FilterModel>()
			where v.id == id && v.userId == userId
			select v).ToListAsync();
	}

	public static async Task<FilterModel> GetFilterById(string id)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<FilterModel> list = await (from v in App.Connection.Table<FilterModel>()
			where v.id == id && v.userId == userId
			select v).ToListAsync();
		if (list != null && list.Count > 0)
		{
			return list[0];
		}
		return null;
	}

	public static async Task BatchUpdateAffectedFilterOnTagChanged(string original, string revised)
	{
		List<FilterModel> obj = await GetAllFilters();
		List<FilterModel> list = new List<FilterModel>();
		foreach (FilterModel item in obj)
		{
			if (item == null)
			{
				continue;
			}
			string text = "\"" + original + "\"";
			string text2 = "\"" + revised?.ToLower() + "\"";
			if (item.rule != null && item.rule.Contains(text) && !item.rule.Contains(text2))
			{
				item.rule = item.rule.Replace(text, text2);
				if (item.syncStatus != 0)
				{
					item.syncStatus = 1;
				}
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			await BatchUpdateFilter(list);
		}
	}

	public static async Task BatchUpdateFilterOnTagDeleted(string tag)
	{
		List<FilterModel> obj = await GetAllFilters();
		List<FilterModel> list = new List<FilterModel>();
		foreach (FilterModel item in obj)
		{
			FilterViewModel filterViewModel = new FilterViewModel(item);
			switch (filterViewModel.Mode)
			{
			case FilterMode.Normal:
			{
				NormalFilterViewModel normalViewModel = filterViewModel.NormalViewModel;
				if (normalViewModel.Tags.Contains(tag))
				{
					normalViewModel.Tags.Remove(tag);
					item.rule = normalViewModel.ToRule(isSave: false);
					list.Add(item);
				}
				break;
			}
			case FilterMode.Advanced:
			{
				AdvancedFilterViewModel advancedViewModel = filterViewModel.AdvancedViewModel;
				if (advancedViewModel.CardList == null || advancedViewModel.CardList.Count <= 0)
				{
					break;
				}
				foreach (CardViewModel card in advancedViewModel.CardList)
				{
					if (card is TagCardViewModel tagCardViewModel && tagCardViewModel.Values.Contains(tag))
					{
						tagCardViewModel.Values.Remove(tag);
						if (tagCardViewModel.Values.Count == 0)
						{
							advancedViewModel.CardList.Remove(tagCardViewModel);
						}
						item.rule = advancedViewModel.ToRule(isSave: false);
						list.Add(item);
						break;
					}
				}
				break;
			}
			}
		}
		if (list.Count > 0)
		{
			await BatchUpdateFilter(list);
		}
	}

	public static bool CheckNameValid(string name, string id)
	{
		name = name.Trim();
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (name.StartsWith("#"))
		{
			return false;
		}
		if (!NameUtils.IsValidNameNoCheckSharp(name, checkSpace: false))
		{
			return false;
		}
		FilterModel filterModel = CacheManager.GetFilters().FirstOrDefault((FilterModel f) => f.name == name);
		if (string.IsNullOrEmpty(id))
		{
			if (filterModel != null)
			{
				return false;
			}
		}
		else if (filterModel != null && filterModel.id != id)
		{
			return false;
		}
		return true;
	}

	public static async Task SaveFilterFromPreview(FilterModel filter)
	{
		if (string.IsNullOrEmpty(filter.id))
		{
			filter.id = Utils.GetGuid();
			filter.createdTime = DateTime.Now;
			filter.modifiedTime = DateTime.Now;
			filter.syncStatus = 0;
			filter.sortOrder = await GetNextFilterSortOrder();
			await AddFilter(filter);
		}
		else
		{
			filter.modifiedTime = DateTime.Now;
			if (filter.syncStatus != 0)
			{
				filter.syncStatus = 1;
			}
			await FilterSyncJsonDao.TrySaveFilter(filter.id);
			await UpdateFilter(filter);
		}
		MainWindowManager.TrySelectFilter(filter);
	}

	internal static async Task SwitchViewModel(FilterModel filter, string vm)
	{
		filter.viewMode = vm;
		if (filter.syncStatus != 0)
		{
			filter.syncStatus = 1;
		}
		await FilterSyncJsonDao.TrySaveFilter(filter.id);
		await UpdateFilter(filter);
	}
}
