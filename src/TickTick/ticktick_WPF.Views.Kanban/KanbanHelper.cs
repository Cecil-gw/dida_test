using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Views.Kanban;

public class KanbanHelper
{
	public static bool IsProjectInKanbanMode(string projectId)
	{
		ProjectModel projectModel = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.id == projectId);
		if (projectModel != null)
		{
			return projectModel.viewMode == "kanban";
		}
		return false;
	}
}
