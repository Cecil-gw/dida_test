using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class ProjectGroupTransfer
{
	public static SyncProjectGroupBean DescribeSyncProjectGroupBean(List<ProjectGroupModel> localChanges)
	{
		SyncProjectGroupBean syncProjectGroupBean = new SyncProjectGroupBean();
		HashSet<string> hashSet = new HashSet<string>();
		if (localChanges != null)
		{
			foreach (ProjectGroupModel localChange in localChanges)
			{
				if (hashSet.Contains(localChange.id))
				{
					App.Connection.DeleteAsync(localChange);
				}
				if (localChange.sync_status.Equals("SYNC_NEW"))
				{
					syncProjectGroupBean.Add.Add(localChange);
					hashSet.Add(localChange.id);
				}
				else if (localChange.sync_status.Equals("SYNC_UPDATE") || localChange.sync_status.Equals("SYNC_INIT"))
				{
					syncProjectGroupBean.Update.Add(localChange);
					hashSet.Add(localChange.id);
				}
			}
		}
		return syncProjectGroupBean;
	}
}
