using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class TaskTypeEditDialog : ConditionEditDialogBase<string>
{
	private List<string> _originTaskTypes;

	public event EventHandler<List<string>> OnSelectedTypesChanged;

	public TaskTypeEditDialog(ICollection<string> selected)
	{
		InitializeComponent();
		InitData(selected);
		base.OnSelectedValueChanged += delegate
		{
			this.OnSelectedTypesChanged?.Invoke(this, GetSelectedValues());
		};
		_originTaskTypes = selected.ToList();
	}

	public TaskTypeEditDialog()
		: this(new List<string>())
	{
	}

	protected virtual void InitData(ICollection<string> selected)
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
					Value = "note",
					Title = Utils.GetString("Notes"),
					Icon = "Ic.ProjectList.Note",
					Selected = selected.Contains("note"),
					ShowIcon = true
				}
			},
			SupportedLogic = new List<LogicType> { LogicType.Or }
		};
	}

	protected override void SyncOriginal()
	{
		_originTaskTypes = GetSelectedValues().ToList();
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
		this.OnSelectedTypesChanged?.Invoke(this, _originTaskTypes);
	}

	private void RestoreSetSelected()
	{
		foreach (FilterListItemViewModel item in base.ViewModel.ItemsSource)
		{
			item.Selected = _originTaskTypes.Contains((string)item.Value);
		}
	}
}
