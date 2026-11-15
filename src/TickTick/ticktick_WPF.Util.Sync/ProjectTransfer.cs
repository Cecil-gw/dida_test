using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util.Sync;

public class ProjectTransfer
{
	public static async Task<SyncProjectBean> DescribeSyncProjectBean(Dictionary<string, string> localChanges)
	{
		SyncProjectBean syncBean = new SyncProjectBean();
		if (localChanges != null && localChanges.Count > 0)
		{
			List<string> teamIds = (from t in CacheManager.GetTeams()
				select t.id).ToList();
			List<ProjectModel> source = (UserDao.IsPro() ? (await ProjectDao.GetAllProjects(withInbox: false, withClosed: false)).Where((ProjectModel p) => !TeamDao.IsTeamExpired(p.teamId)).ToList() : (await ProjectDao.GetAllProjects(withInbox: false)).ToList());
			int count = source.Where((ProjectModel p) => (string.IsNullOrEmpty(p.teamId) || teamIds.Contains(p.teamId)) && p.sync_status != Constants.SyncStatus.SYNC_ERROR_UP_LIMIT.ToString() && p.sync_status != Constants.SyncStatus.SYNC_NEW.ToString()).ToList().Count;
			long canAddProjectCount = Utils.GetUserLimit(Constants.LimitKind.ProjectNumber) - count;
			foreach (KeyValuePair<string, string> local in localChanges)
			{
				if (local.Key.Contains("inbox"))
				{
					continue;
				}
				ProjectModel projectModel = await ProjectDao.GetProjectById(local.Key);
				if (projectModel == null)
				{
					continue;
				}
				if (local.Value == Constants.SyncStatus.SYNC_NEW.ToString() || local.Value == Constants.SyncStatus.SYNC_ERROR_UP_LIMIT.ToString())
				{
					if (canAddProjectCount > 0)
					{
						syncBean.Add.Add(projectModel);
						canAddProjectCount--;
					}
					else if (local.Value == Constants.SyncStatus.SYNC_NEW.ToString())
					{
						projectModel.sync_status = Constants.SyncStatus.SYNC_ERROR_UP_LIMIT.ToString();
						await App.Connection.UpdateAsync(projectModel);
					}
				}
				else if (local.Value == Constants.SyncStatus.SYNC_UPDATE.ToString() || local.Value == Constants.SyncStatus.SYNC_INIT.ToString())
				{
					if (projectModel.delete_status)
					{
						syncBean.Deleted.Add(local.Key);
					}
					else
					{
						syncBean.Update.Add(projectModel);
					}
				}
			}
		}
		return syncBean;
	}
}
