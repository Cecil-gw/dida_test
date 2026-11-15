using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views.Misc;

namespace ticktick_WPF.Views.QuickAdd;

public class ProjectInputItems : BaseInputItems<ProjectModel>
{
	private string _selected;

	public ProjectInputItems(string projectId)
	{
		_selected = projectId;
		LoadData();
	}

	protected override ObservableCollection<InputItemViewModel<ProjectModel>> InitData()
	{
		List<ProjectModel> projects = CacheManager.GetProjects();
		ObservableCollection<InputItemViewModel<ProjectModel>> observableCollection = new ObservableCollection<InputItemViewModel<ProjectModel>>();
		List<string> list = (from item in ProjectDataAssembler.AssembleProjects(onlyShowPermission: true)
			where item is ProjectViewModel
			select item.Id).ToList();
		if (LocalSettings.Settings.SmartListInbox != 1)
		{
			ProjectModel projectModel = projects.FirstOrDefault((ProjectModel p) => p.Isinbox);
			if (projectModel != null)
			{
				projectModel.name = Utils.GetString("Inbox");
				observableCollection.Add(new InputItemViewModel<ProjectModel>(projectModel.name, projectModel));
			}
		}
		foreach (string id in list)
		{
			ProjectModel projectModel2 = projects.FirstOrDefault((ProjectModel p) => p.id == id);
			if (projectModel2 != null)
			{
				observableCollection.Add(new InputItemViewModel<ProjectModel>(projectModel2.name, projectModel2)
				{
					HighLightSelected = (projectModel2.id == _selected)
				});
			}
		}
		return observableCollection;
	}

	internal void SetSelected(string projectId)
	{
		_selected = projectId;
		LoadData();
	}
}
