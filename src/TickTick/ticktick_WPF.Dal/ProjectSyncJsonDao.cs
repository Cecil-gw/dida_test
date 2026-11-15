using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.Dal;

public static class ProjectSyncJsonDao
{
	public static async Task<List<ProjectSyncedJsonModel>> GetProjectSyncedModels()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<ProjectSyncedJsonModel>()
			where model.UserId == userId
			select model).ToListAsync();
	}

	private static async Task<ProjectSyncedJsonModel> GetSavedJson(string projectId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return await (from model in App.Connection.Table<ProjectSyncedJsonModel>()
			where model.UserId == userId && model.ProjectId == projectId
			select model).FirstOrDefaultAsync();
	}

	public static async Task TryAddProjectJson(string projectId)
	{
		ProjectModel project = await ProjectDao.GetProjectById(projectId);
		if (project != null)
		{
			string userId = Utils.GetCurrentUserIdInt().ToString();
			ProjectSyncedJsonModel jsonModel = new ProjectSyncedJsonModel
			{
				UserId = Utils.GetCurrentUserIdInt().ToString(),
				ProjectId = project.id,
				JsonString = JsonConvert.SerializeObject(project)
			};
			ProjectSyncedJsonModel projectSyncedJsonModel = await (from p in App.Connection.Table<ProjectSyncedJsonModel>()
				where p.ProjectId == project.id && p.UserId == userId
				select p).FirstOrDefaultAsync();
			if (projectSyncedJsonModel == null)
			{
				await App.Connection.InsertAsync(jsonModel);
				return;
			}
			jsonModel._Id = projectSyncedJsonModel._Id;
			await App.Connection.UpdateAsync(jsonModel);
		}
	}

	public static async Task BatchDeleteProjects(List<string> projectIds)
	{
		if (projectIds == null || !projectIds.Any())
		{
			return;
		}
		foreach (string projectId in projectIds)
		{
			ProjectSyncedJsonModel projectSyncedJsonModel = await GetSavedJson(projectId);
			if (projectSyncedJsonModel != null)
			{
				await App.Connection.DeleteAsync(projectSyncedJsonModel);
			}
		}
	}

	public static async Task BatchDeleteProjects(List<ProjectModel> projects)
	{
		if (projects != null && projects.Any())
		{
			await BatchDeleteProjects(projects.Select((ProjectModel p) => p.id).ToList());
		}
	}
}
