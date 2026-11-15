using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class PriorityEditDialog : ConditionEditDialogBase<int>
{
	private List<int> _originPriorities;

	public event EventHandler<List<int>> OnSelectedPriorityChanged;

	public PriorityEditDialog(List<int> selectedPriorities, bool showAll = false)
	{
		InitializeComponent();
		InitData(selectedPriorities, showAll);
		base.OnSelectedValueChanged += delegate(object sender, FilterConditionViewModel e)
		{
			if (e != null && e.ItemsSource.All((FilterListItemViewModel item) => item.Selected))
			{
				foreach (FilterListItemViewModel item in e.ItemsSource)
				{
					item.Selected = false;
				}
				e.IsAllSelected = true;
			}
			this.OnSelectedPriorityChanged?.Invoke(this, GetSelectedValues());
		};
		_originPriorities = selectedPriorities.ToList();
	}

	public PriorityEditDialog()
		: this(new List<int>())
	{
	}

	private void InitData(ICollection<int> selectedPriorities, bool showAll)
	{
		base.ViewModel = new FilterConditionViewModel
		{
			Type = CondType.Priority,
			ItemsSource = new ObservableCollection<FilterListItemViewModel>
			{
				new FilterListItemViewModel
				{
					Value = 5,
					Title = Utils.GetString("PriorityHigh"),
					Selected = selectedPriorities.Contains(5),
					ShowIcon = true,
					IsPriorityIcon = true
				},
				new FilterListItemViewModel
				{
					Value = 3,
					Title = Utils.GetString("PriorityMedium"),
					Selected = selectedPriorities.Contains(3),
					ShowIcon = true,
					IsPriorityIcon = true
				},
				new FilterListItemViewModel
				{
					Value = 1,
					Title = Utils.GetString("PriorityLow"),
					Selected = selectedPriorities.Contains(1),
					ShowIcon = true,
					IsPriorityIcon = true
				},
				new FilterListItemViewModel
				{
					Value = 0,
					Title = Utils.GetString("PriorityNull"),
					Selected = selectedPriorities.Contains(0),
					ShowIcon = true,
					IsPriorityIcon = true
				}
			},
			SupportedLogic = new List<LogicType> { LogicType.Or },
			ShowAll = showAll,
			AllIconVisible = Visibility.Collapsed,
			IsAllSelected = !selectedPriorities.Any()
		};
	}

	protected override void SyncOriginal()
	{
		_originPriorities = GetSelectedValues().ToList();
	}

	protected override bool CanSave()
	{
		if (GetSelectedValues().Count <= 0)
		{
			return base.CanSave();
		}
		return true;
	}

	public override void Restore()
	{
		RestoreSetSelected();
		this.OnSelectedPriorityChanged?.Invoke(this, _originPriorities);
	}

	private void RestoreSetSelected()
	{
		foreach (FilterListItemViewModel item in base.ViewModel.ItemsSource)
		{
			item.Selected = _originPriorities.Contains((int)item.Value);
		}
	}
}
