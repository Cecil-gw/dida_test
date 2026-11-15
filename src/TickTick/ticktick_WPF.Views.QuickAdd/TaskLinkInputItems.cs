using System.Collections.Generic;
using System.Collections.ObjectModel;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class TaskLinkInputItems : BaseInputItems<TaskBaseViewModel>
{
	public List<TaskBaseViewModel> Models;

	public TaskLinkInputItems(List<TaskBaseViewModel> models, bool selectFirst = true)
	{
		Models = models;
		base.Width = 220.0;
		LoadData(selectFirst);
	}

	protected override ObservableCollection<InputItemViewModel<TaskBaseViewModel>> InitData()
	{
		if (Models != null)
		{
			ObservableCollection<InputItemViewModel<TaskBaseViewModel>> observableCollection = new ObservableCollection<InputItemViewModel<TaskBaseViewModel>>();
			{
				foreach (TaskBaseViewModel model in Models)
				{
					observableCollection.Add(new InputItemViewModel<TaskBaseViewModel>(model.Title, model.Id, model, needPy: false));
				}
				return observableCollection;
			}
		}
		return null;
	}

	public void SetModels(List<TaskBaseViewModel> tasks)
	{
		Models = tasks;
		LoadData();
	}
}
