using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Provider;

public class FilterPreviewIdentity : ProjectIdentity
{
	public FilterModel Filter { get; }

	public FilterPreviewIdentity(FilterModel filter)
	{
		Filter = filter;
		base.SortOption = filter.GetSortOption();
	}
}
