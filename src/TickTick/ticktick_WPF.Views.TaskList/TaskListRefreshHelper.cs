using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.Provider;
using ticktick_WPF.Util.Sort;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.TaskList;

public static class TaskListRefreshHelper
{
	public static async Task<List<TaskBaseViewModel>> InitData(ProjectIdentity identity, bool withChild = true)
	{
		if (identity == null)
		{
			return new List<TaskBaseViewModel>();
		}
		return await Task.Run(async () => await ProjectTaskDataProvider.GetDisplayModels(identity, withChild));
	}

	public static async Task<List<DisplayItemModel>> GetSortedDisplayItems(ProjectIdentity identity, List<TaskBaseViewModel> displayModels, bool showLoadMore = false, List<ColumnModel> columns = null, ClosedLoader completedTaskLoader = null, bool forceReadOnly = false)
	{
		if (identity == null)
		{
			return new List<DisplayItemModel>();
		}
		if (displayModels != null)
		{
			ObservableCollection<DisplayItemModel> models = BuildDisplayItemModels(identity, displayModels, forceReadOnly);
			completedTaskLoader?.GetCompletedLimit();
			if (!(identity is ColumnProjectIdentity columnProjectIdentity))
			{
				_ = identity.SortOption;
			}
			else
			{
				columnProjectIdentity.GetRealSortOption();
			}
			bool buildByChildIds = ShouldBuildByChildIds(identity);
			return (await SortHelper.Sort(models, identity, showFold: true, showLoadMore, columns, completedTaskLoader, null, buildByChildIds)).ToList();
		}
		return new List<DisplayItemModel>();
	}

	private static bool ShouldBuildByChildIds(ProjectIdentity identity)
	{
		if (identity is ColumnProjectIdentity columnProjectIdentity)
		{
			return columnProjectIdentity.Project is NormalProjectIdentity;
		}
		return identity is NormalProjectIdentity;
	}

	private static ObservableCollection<DisplayItemModel> BuildDisplayItemModels(ProjectIdentity identity, IEnumerable<TaskBaseViewModel> models, bool forceReadOnly)
	{
		bool canDrag = identity.CanDrag;
		ObservableCollection<DisplayItemModel> observableCollection = new ObservableCollection<DisplayItemModel>();
		if (models != null)
		{
			foreach (TaskBaseViewModel model in models)
			{
				DisplayItemModel displayItemModel;
				switch (model.Type)
				{
				case DisplayType.Habit:
					displayItemModel = new DisplayItemModel(model);
					break;
				case DisplayType.Event:
					displayItemModel = new DisplayItemModel(model);
					break;
				default:
				{
					bool isKanbanMode = false;
					bool showProject = !(identity is NormalProjectIdentity);
					if (identity is ColumnProjectIdentity columnProjectIdentity)
					{
						isKanbanMode = true;
						showProject = !(columnProjectIdentity.Project is NormalProjectIdentity);
					}
					displayItemModel = new DisplayItemModel(model, showProject, isKanbanMode);
					break;
				}
				}
				if (!canDrag || (model.IsCourse && identity.SortOption.groupByType != Constants.SortType.dueDate))
				{
					displayItemModel.ShowDragBar = false;
				}
				displayItemModel.InTomorrow = identity is TomorrowProjectIdentity;
				displayItemModel.InMatrix = identity is MatrixQuadrantIdentity;
				if (forceReadOnly)
				{
					displayItemModel.SetForceReadOnly(readOnly: true);
				}
				observableCollection.Add(displayItemModel);
			}
		}
		return observableCollection;
	}

	public static bool CheckChanged(List<TaskBaseViewModel> newModels, List<TaskBaseViewModel> models)
	{
		if (newModels.Count != models.Count)
		{
			return true;
		}
		Dictionary<string, TaskBaseViewModel> dictionary = new Dictionary<string, TaskBaseViewModel>();
		foreach (TaskBaseViewModel newModel in newModels)
		{
			if (!string.IsNullOrEmpty(newModel.Id))
			{
				dictionary[newModel.Id] = newModel;
			}
		}
		foreach (TaskBaseViewModel model2 in models)
		{
			if (!string.IsNullOrEmpty(model2.Id) && dictionary.ContainsKey(model2.Id))
			{
				if (model2.IsCourse || model2.IsEvent || model2.Type == DisplayType.Habit)
				{
					TaskBaseViewModel model = dictionary[model2.Id];
					if (!model2.IsPropertiesEqual(model))
					{
						return true;
					}
				}
				continue;
			}
			return true;
		}
		return false;
	}
}
