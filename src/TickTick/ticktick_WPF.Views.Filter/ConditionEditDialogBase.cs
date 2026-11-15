using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public abstract class ConditionEditDialogBase<T> : ConditionEditDialog
{
	protected virtual List<T> GetSelectedValues()
	{
		if (base.ViewModel != null)
		{
			if (base.ViewModel.IsAllSelected)
			{
				return new List<T>();
			}
			List<T> selectedValues = new List<T>();
			foreach (FilterListItemViewModel item2 in base.ViewModel.ItemsSource)
			{
				if (item2.IsAssignee)
				{
					if (item2.Selected)
					{
						T item = (T)item2.Value;
						selectedValues.Add(item);
						if (item.ToString() == "other")
						{
							break;
						}
					}
					continue;
				}
				if (item2.Selected && !item2.IsSecondLevel && (!item2.IsProjectGroup || item2.IsTagParent))
				{
					selectedValues.Add((T)item2.Value);
				}
				if (!item2.IsProjectGroup)
				{
					continue;
				}
				item2.Children?.ForEach(delegate(FilterListItemViewModel filterListItemViewModel)
				{
					if (filterListItemViewModel.Selected)
					{
						selectedValues.Add((T)filterListItemViewModel.Value);
					}
				});
			}
			return selectedValues;
		}
		return new List<T>();
	}
}
