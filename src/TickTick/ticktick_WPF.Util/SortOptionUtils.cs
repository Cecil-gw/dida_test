using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

public class SortOptionUtils
{
	public static SortOption GetSortOptionBySortType(Constants.SortType sortType, bool inKanban)
	{
		SortOption sortOption = new SortOption();
		if (inKanban)
		{
			sortOption.groupBy = "sortOrder";
			sortOption.orderBy = sortType.ToString();
		}
		else if (sortType == Constants.SortType.title || sortType == Constants.SortType.createdTime || sortType == Constants.SortType.modifiedTime)
		{
			sortOption.groupBy = "none";
			sortOption.orderBy = sortType.ToString();
		}
		else
		{
			sortOption.groupBy = sortType.ToString();
			sortOption.orderBy = ((sortType == Constants.SortType.sortOrder) ? sortType.ToString() : "dueDate");
		}
		return sortOption;
	}
}
