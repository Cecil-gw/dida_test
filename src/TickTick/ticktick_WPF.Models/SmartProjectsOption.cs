using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Views.MainListView;

namespace ticktick_WPF.Models;

public class SmartProjectsOption : PreferenceBaseModel
{
	[JsonProperty("smartProjects")]
	public List<SmartProjectOption> SmartProjects { get; set; }

	public override bool SetRemoteValue(PreferenceBaseModel model)
	{
		if (model is SmartProjectsOption smartProjectsOption)
		{
			if (smartProjectsOption.mtime > base.mtime)
			{
				base.mtime = smartProjectsOption.mtime;
				SaveProjects(smartProjectsOption.SmartProjects);
				ListViewContainer.ReloadTasks();
			}
			else if (smartProjectsOption.mtime < base.mtime)
			{
				return true;
			}
		}
		return false;
	}

	private void SaveProjects(List<SmartProjectOption> remotes)
	{
		if (SmartProjects == null)
		{
			SmartProjects = remotes;
			return;
		}
		foreach (SmartProjectOption remote in remotes)
		{
			SmartProjects.FirstOrDefault((SmartProjectOption m) => m.Name == remote.Name)?.SaveRemote(remote);
		}
	}

	public string GetViewModeByName(string name)
	{
		return SmartProjects?.FirstOrDefault((SmartProjectOption s) => s.Name == name)?.ViewMode;
	}

	public SortOption GetSortOptionByName(string name)
	{
		return SmartProjects?.FirstOrDefault((SmartProjectOption s) => s.Name == name)?.SortOption;
	}

	public SmartProjectOption GetSmartProjectByName(string name)
	{
		return SmartProjects?.FirstOrDefault((SmartProjectOption s) => s.Name == name);
	}

	public TimelineModel GetTimeline(string name)
	{
		SmartProjectOption smartProjectByName = GetSmartProjectByName(name);
		TimelineModel timelineModel = smartProjectByName?.Timeline;
		if (timelineModel == null)
		{
			timelineModel = ((name == "inbox") ? new TimelineModel("sortOrder") : new TimelineModel("project"));
			if (smartProjectByName != null)
			{
				smartProjectByName.Timeline = timelineModel;
			}
		}
		return timelineModel;
	}

	public void SaveViewMode(string name, string viewMode)
	{
		SmartProjectOption smartProjectByName = GetSmartProjectByName(name);
		if (smartProjectByName != null)
		{
			smartProjectByName.ViewMode = viewMode;
			return;
		}
		if (SmartProjects == null)
		{
			SmartProjects = new List<SmartProjectOption>();
		}
		SmartProjects.Add(new SmartProjectOption
		{
			Name = name,
			ViewMode = viewMode
		});
	}
}
