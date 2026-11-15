using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class ProjectGroupSyncedJsonDao
{
	public static async Task TrySaveProjectGroup(string groupId)
	{
		if (await GetSavedJson(groupId) == null)
		{
			ProjectGroupModel projectGroupModel = await ProjectGroupDao.GetProjectGroupById(groupId);
			if (projectGroupModel != null)
			{
				ProjectGroupSyncedJsonModel item = new ProjectGroupSyncedJsonModel
				{
					GroupId = groupId,
					UserId = Utils.GetCurrentUserIdInt().ToString(),
					JsonString = JsonConvert.SerializeObject(projectGroupModel)
				};
				await App.Connection.InsertAsync(item);
			}
		}
	}

	public static async Task<ProjectGroupSyncedJsonModel> GetSavedJson(string groupId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<ProjectGroupSyncedJsonModel>()
			where model.UserId == userId && model.GroupId == groupId
			select model).FirstOrDefaultAsync();
	}

	public static async Task BatchDeleteGroups(List<string> groupIds)
	{
		if (groupIds == null || !groupIds.Any())
		{
			return;
		}
		foreach (string groupId in groupIds)
		{
			ProjectGroupSyncedJsonModel projectGroupSyncedJsonModel = await GetSavedJson(groupId);
			if (projectGroupSyncedJsonModel != null)
			{
				await App.Connection.DeleteAsync(projectGroupSyncedJsonModel);
			}
		}
	}
}
