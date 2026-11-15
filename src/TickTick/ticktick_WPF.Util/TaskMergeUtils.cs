using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util;

public static class TaskMergeUtils
{
	public static async Task<ProjectModel> GetMergedProject(List<TaskModel> tasks)
	{
		List<string> projectIds = tasks.Select((TaskModel task) => task.projectId).Distinct().ToList();
		if (projectIds.Count == 1)
		{
			ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectIds[0]);
			if (projectModel != null)
			{
				return projectModel;
			}
		}
		return new ProjectModel
		{
			id = Utils.GetInboxId(),
			name = Utils.GetString("Inbox")
		};
	}
}
