using System.Collections.ObjectModel;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.QuickAdd;

public class PriorityInputItems : BaseInputItems<int>
{
	private int _selected;

	public PriorityInputItems(int selected = -1)
	{
		_selected = selected;
		LoadData();
	}

	protected override ObservableCollection<InputItemViewModel<int>> InitData()
	{
		return new ObservableCollection<InputItemViewModel<int>>
		{
			new InputItemViewModel<int>(Utils.GetString("PriorityHigh"), 5)
			{
				HighLightSelected = (_selected == 5)
			},
			new InputItemViewModel<int>(Utils.GetString("PriorityMedium"), 3)
			{
				HighLightSelected = (_selected == 3)
			},
			new InputItemViewModel<int>(Utils.GetString("PriorityLow"), 1)
			{
				HighLightSelected = (_selected == 1)
			},
			new InputItemViewModel<int>(Utils.GetString("PriorityNull"), 0)
			{
				HighLightSelected = (_selected == 0)
			}
		};
	}

	internal void SetSelected(int priority)
	{
		_selected = priority;
		LoadData();
	}
}
