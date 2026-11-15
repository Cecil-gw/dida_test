using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Search;

public class SearchExtra
{
	public string SearchKey;

	public List<string> Tags;

	public string SearchId;

	public List<string> LinkTaskIds;

	public SearchCategory Category;

	public SearchFilterModel Filter;

	public string from;

	public SearchExtra()
	{
	}

	public SearchExtra(SearchHistoryModel model)
	{
		SearchKey = model.keyText;
		Tags = model.tags?.Split('#').ToList() ?? new List<string>();
		Tags?.Remove("");
	}

	public bool Empty()
	{
		if (string.IsNullOrWhiteSpace(SearchKey) && (Tags == null || Tags.Count == 0) && (Filter == null || !Filter.HasFilterConditions()))
		{
			return Category == SearchCategory.None;
		}
		return false;
	}
}
