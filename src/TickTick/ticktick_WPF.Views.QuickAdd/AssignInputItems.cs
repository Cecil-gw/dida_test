using System.Collections.Generic;
using System.Collections.ObjectModel;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.QuickAdd;

public class AssignInputItems : BaseInputItems<AvatarViewModel>
{
	public List<AvatarViewModel> Models;

	private string _selected;

	public AssignInputItems(List<AvatarViewModel> models, bool selectFirst = true, string selected = null)
	{
		Models = models;
		_selected = selected;
		LoadData(selectFirst);
	}

	protected override ObservableCollection<InputItemViewModel<AvatarViewModel>> InitData()
	{
		if (Models != null)
		{
			ObservableCollection<InputItemViewModel<AvatarViewModel>> observableCollection = new ObservableCollection<InputItemViewModel<AvatarViewModel>>();
			{
				foreach (AvatarViewModel model in Models)
				{
					observableCollection.Add(new InputItemViewModel<AvatarViewModel>(model.Name, model.UserId, model.AvatarUrl, model)
					{
						IsNoAvatar = model.IsNoAvatar,
						HighLightSelected = (model.UserId == _selected || (string.IsNullOrEmpty(_selected) && model.UserId == "-1"))
					});
				}
				return observableCollection;
			}
		}
		return null;
	}

	public void SetModels(List<AvatarViewModel> avatars)
	{
		Models = avatars;
		LoadData();
	}

	internal void SetSelected(string assignee)
	{
		_selected = assignee;
		LoadData();
	}
}
