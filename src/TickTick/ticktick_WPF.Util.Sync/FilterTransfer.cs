using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public static class FilterTransfer
{
	public static SyncFilterBean DescribeSyncFilterBean(List<FilterModel> localChanges)
	{
		SyncFilterBean syncFilterBean = new SyncFilterBean();
		foreach (FilterModel localChange in localChanges)
		{
			if (localChange.IsLocalAdded())
			{
				syncFilterBean.Add.Add(localChange);
			}
			else if (localChange.IsLocalUpdated())
			{
				syncFilterBean.Update.Add(localChange);
			}
			else if (localChange.IsLocalDeleted())
			{
				syncFilterBean.Deleted.Add(localChange.id);
			}
		}
		return syncFilterBean;
	}
}
