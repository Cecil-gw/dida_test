using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class ListItemChangeArgs<T>
{
	public ListChangeAction Action;

	public List<T> Items;

	public List<T> OldItems;
}
