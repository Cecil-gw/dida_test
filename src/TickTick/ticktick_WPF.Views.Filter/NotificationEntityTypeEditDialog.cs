using System.Collections.Generic;
using System.Collections.ObjectModel;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class NotificationEntityTypeEditDialog : TaskTypeEditDialog
{
	public NotificationEntityTypeEditDialog(List<string> selected)
		: base(selected)
	{
	}

	protected override bool CanSave()
	{
		if (GetSelectedValues().Count <= 0)
		{
			return base.CanSave();
		}
		return true;
	}

	protected override void InitData(ICollection<string> selected)
	{
		base.ViewModel = new FilterConditionViewModel
		{
			Type = CondType.TaskType,
			ItemsSource = new ObservableCollection<FilterListItemViewModel>
			{
				new FilterListItemViewModel
				{
					Value = "task",
					Title = Utils.GetString("Task"),
					Icon = "Ic.ProjectList.List",
					Selected = selected.Contains("task"),
					ShowIcon = true
				},
				new FilterListItemViewModel
				{
					Value = "habit",
					Title = Utils.GetString("Habits"),
					Icon = "Ic.TaskOM.Habit",
					Selected = selected.Contains("habit"),
					ShowIcon = true
				}
			},
			IsAllSelected = false,
			SupportedLogic = new List<LogicType>()
		};
	}
}
