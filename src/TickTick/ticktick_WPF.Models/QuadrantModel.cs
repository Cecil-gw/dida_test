using Newtonsoft.Json;
using ticktick_WPF.Util;

namespace ticktick_WPF.Models;

public class QuadrantModel
{
	public string id { get; set; }

	public string rule { get; set; }

	public string sortType { get; set; }

	public long? sortOrder { get; set; }

	public string name { get; set; }

	[JsonProperty("sortOption")]
	public SortOption SortOption { get; set; }

	public SortOption GetSortOption()
	{
		if (SortOption == null)
		{
			return SortOptionUtils.GetSortOptionBySortType(Utils.LoadSortType(sortType), inKanban: false);
		}
		return SortOption;
	}

	public static QuadrantModel GetDefault(int level)
	{
		return new QuadrantModel
		{
			id = "quadrant" + level,
			sortType = "dueDate",
			name = string.Empty,
			sortOrder = (long)(level - 1) * 268435456L,
			rule = MatrixManager.GetDefaultQuadrantRule(MatrixType.Simple, level)
		};
	}
}
