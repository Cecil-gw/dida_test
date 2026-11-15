using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Resource;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Search;

internal class SearchTaskResultPagingState
{
	private readonly List<TaskBaseViewModel> _items;

	public SearchKind Kind { get; }

	private int NextIndex { get; set; }

	public bool HasMore => NextIndex < _items.Count;

	public SearchTaskResultPagingState(List<TaskBaseViewModel> items, SearchKind kind, int nextIndex)
	{
		_items = items;
		Kind = kind;
		NextIndex = nextIndex;
	}

	public IEnumerable<TaskBaseViewModel> TakeNext(int count)
	{
		List<TaskBaseViewModel> list = _items.Skip(NextIndex).Take(count).ToList();
		NextIndex += list.Count;
		return list;
	}
}
