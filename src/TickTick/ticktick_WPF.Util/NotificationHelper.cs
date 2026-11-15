using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util;

public static class NotificationHelper
{
	private static readonly Dictionary<string, string[]> ProjectNtfOptions = new Dictionary<string, string[]>();

	private static bool _initialized;

	public static async void InitProjectNtfOptions()
	{
		if (_initialized)
		{
			return;
		}
		foreach (ProjectModel item in (from project in CacheManager.GetProjects()
			where project.IsEnable()
			select project).ToList())
		{
			if (ProjectNtfOptions.ContainsKey(item.id))
			{
				ProjectNtfOptions[item.id] = item.notificationOptions;
			}
			else
			{
				ProjectNtfOptions.Add(item.id, item.notificationOptions);
			}
		}
		_initialized = true;
	}

	public static string[] GetNtfOptions(string projectId)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			return null;
		}
		if (!ProjectNtfOptions.ContainsKey(projectId))
		{
			return null;
		}
		return ProjectNtfOptions[projectId];
	}

	public static void OnProjectChanged(ProjectModel project)
	{
		if (project.IsEnable())
		{
			SaveNtfOptions(project.id, project.notificationOptions);
		}
		else
		{
			ProjectNtfOptions.Remove(project.id);
		}
	}

	public static void SaveNtfOptions(string projectId, string[] ntfOptions)
	{
		if (ProjectNtfOptions.ContainsKey(projectId))
		{
			ProjectNtfOptions[projectId] = ntfOptions;
		}
		else
		{
			ProjectNtfOptions.Add(projectId, ntfOptions);
		}
	}
}
