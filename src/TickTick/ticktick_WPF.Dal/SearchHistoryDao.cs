using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.Search;

namespace ticktick_WPF.Dal;

public static class SearchHistoryDao
{
	private static List<SearchExtra> _searchHistoryModels;

	private static List<SearchHistoryModel> _historyModles = new List<SearchHistoryModel>();

	public static async Task GetSearchHistoryModels()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		_historyModles = await (from m in App.Connection.Table<SearchHistoryModel>()
			where m.userId == userId && m.keyText != null
			orderby m.sortOrder descending
			select m).ToListAsync();
		_searchHistoryModels = new List<SearchExtra>();
		if (_historyModles == null)
		{
			return;
		}
		for (int num = 0; num < _historyModles.Count; num++)
		{
			if (num < 5)
			{
				_searchHistoryModels.Add(new SearchExtra(_historyModles[num]));
			}
			else
			{
				App.Connection.DeleteAsync(_historyModles[num]);
			}
		}
	}

	public static async Task AddHistoryModel(string key, List<string> tags)
	{
		if (_searchHistoryModels == null)
		{
			_searchHistoryModels = new List<SearchExtra>();
		}
		key = key ?? string.Empty;
		tags = tags ?? new List<string>();
		SearchExtra searchExtra = _searchHistoryModels.FirstOrDefault((SearchExtra m) => (m.SearchKey?.Trim() ?? "") == key.Trim() && !m.Tags.Except(tags).Any() && !tags.Except(m.Tags).Any());
		if (searchExtra != null)
		{
			int index = _searchHistoryModels.IndexOf(searchExtra);
			_searchHistoryModels.Remove(searchExtra);
			_searchHistoryModels.Insert(0, searchExtra);
			SearchHistoryModel searchHistoryModel = _historyModles[index];
			_historyModles.Remove(searchHistoryModel);
			_historyModles.Insert(0, searchHistoryModel);
			searchHistoryModel.sortOrder = Utils.GetNowTimeStamp();
			App.Connection.UpdateAsync(searchHistoryModel);
			return;
		}
		string userId = Utils.GetCurrentUserIdInt().ToString();
		long nowTimeStamp = Utils.GetNowTimeStamp();
		SearchHistoryModel searchHistoryModel2 = new SearchHistoryModel
		{
			userId = userId,
			keyText = key.Trim(),
			tags = tags.Join("#"),
			sortOrder = nowTimeStamp
		};
		_searchHistoryModels.Insert(0, new SearchExtra(searchHistoryModel2));
		_historyModles.Insert(0, searchHistoryModel2);
		if (_searchHistoryModels.Count > 5)
		{
			_searchHistoryModels.RemoveAt(5);
		}
		App.Connection.InsertAsync(searchHistoryModel2);
	}

	public static async Task ClearHistory()
	{
		_searchHistoryModels.Clear();
		string userId = Utils.GetCurrentUserIdInt().ToString();
		foreach (SearchHistoryModel item in await (from m in App.Connection.Table<SearchHistoryModel>()
			where m.userId == userId
			orderby m.sortOrder descending
			select m).ToListAsync())
		{
			await App.Connection.DeleteAsync(item);
		}
	}

	public static async Task<List<SearchExtra>> GetHistoryViewModels()
	{
		if (_searchHistoryModels == null || _searchHistoryModels.Count == 0)
		{
			await GetSearchHistoryModels();
		}
		return _searchHistoryModels;
	}

	public static void Clear()
	{
		_searchHistoryModels?.Clear();
	}
}
