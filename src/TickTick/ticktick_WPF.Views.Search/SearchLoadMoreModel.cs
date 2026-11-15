using System.Collections.Generic;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Views.Search;

public class SearchLoadMoreModel : SearchItemBaseModel
{
	public bool IsTask;

	public SearchCategory Category;

	public PtfType Type = PtfType.Null;

	public List<SearchTagAndProjectModel> Children;
}
