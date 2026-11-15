using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class TaskPinnedSortOrderModelComparer : IEqualityComparer<SyncSortOrderModel>
{
	public bool Equals(SyncSortOrderModel x, SyncSortOrderModel y)
	{
		if (x == null && y == null)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		return x.EntityId == y.EntityId;
	}

	public int GetHashCode(SyncSortOrderModel obj)
	{
		return obj.EntityId.GetHashCode();
	}
}
