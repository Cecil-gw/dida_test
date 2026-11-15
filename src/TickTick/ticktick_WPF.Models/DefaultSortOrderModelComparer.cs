using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class DefaultSortOrderModelComparer : IEqualityComparer<SyncSortOrderModel>
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
		if (x.EntityId == y.EntityId)
		{
			return x.GroupId == y.GroupId;
		}
		return false;
	}

	public int GetHashCode(SyncSortOrderModel obj)
	{
		return obj.EntityId.GetHashCode();
	}
}
