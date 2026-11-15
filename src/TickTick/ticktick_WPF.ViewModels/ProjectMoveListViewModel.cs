using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using ticktick_WPF.Models;
using ticktick_WPF.Util;

namespace ticktick_WPF.ViewModels;

[Serializable]
public class ProjectMoveListViewModel : BaseViewModel
{
	public enum projectMoveItemType
	{
		group,
		project
	}

	public int _Id { get; set; }

	public string id { get; set; }

	public long sortOrder { get; set; }

	public int userCount { get; set; }

	public projectMoveItemType type { get; set; }

	private string name { get; set; }

	private string color { get; set; }

	public string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
			OnPropertyChanged("Name");
		}
	}

	public string Color
	{
		get
		{
			if (color == null)
			{
				return ThemeUtil.GetColor("BaseColorOpacity60").ToString();
			}
			return color;
		}
		set
		{
			color = value;
			OnPropertyChanged("Color");
		}
	}

	public static ObservableCollection<ProjectMoveListViewModel> GetProjectUseToOrderList(ObservableCollection<ProjectModel> projectMoveSaveList, ObservableCollection<ProjectGroupModel> projectGroupMoveSaveList)
	{
		ObservableCollection<ProjectMoveListViewModel> observableCollection = new ObservableCollection<ProjectMoveListViewModel>();
		foreach (ProjectGroupModel projectGroupMoveSave in projectGroupMoveSaveList)
		{
			observableCollection.Add(new ProjectMoveListViewModel
			{
				_Id = projectGroupMoveSave._Id,
				id = projectGroupMoveSave.id,
				Name = projectGroupMoveSave.name,
				type = projectMoveItemType.group,
				sortOrder = projectGroupMoveSave.sortOrder.GetValueOrDefault()
			});
		}
		foreach (ProjectModel projectMoveSave in projectMoveSaveList)
		{
			if ((string.IsNullOrEmpty(projectMoveSave.groupId) || projectMoveSave.groupId == "NONE") && projectMoveSave.closed != true)
			{
				observableCollection.Add(new ProjectMoveListViewModel
				{
					_Id = projectMoveSave._Id,
					id = projectMoveSave.id,
					Name = projectMoveSave.name,
					type = projectMoveItemType.project,
					sortOrder = projectMoveSave.sortOrder,
					userCount = projectMoveSave.userCount
				});
			}
		}
		return new ObservableCollection<ProjectMoveListViewModel>(observableCollection.OrderBy((ProjectMoveListViewModel p) => p.sortOrder).ToList());
	}

	public static int FindUseItem(ItemCollection contextMenuList)
	{
		for (int i = 0; i < contextMenuList.Count; i++)
		{
			if ((contextMenuList[i] as MenuItem).Foreground == ThemeUtil.GetColor("PrimaryColor"))
			{
				return i;
			}
		}
		return 0;
	}
}
