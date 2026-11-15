using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class TaskTypeCard : CardBase
{
	private List<string> SelectedTypes { get; set; } = new List<string>();

	public TaskTypeCard(CardViewModel viewModel)
		: base(viewModel)
	{
		if (EditDialog is TaskTypeEditDialog taskTypeEditDialog)
		{
			taskTypeEditDialog.OnSelectedTypesChanged += delegate(object sender, List<string> selected)
			{
				SelectedTypes = selected;
				((TaskTypeViewModel)ViewModel).Values = selected;
				ViewModel.Content = ViewModel.ToCardText();
			};
			taskTypeEditDialog.OnCancel += delegate
			{
				Popup.IsOpen = false;
			};
			taskTypeEditDialog.OnSave += delegate
			{
				IsSave = true;
				Popup.IsOpen = false;
			};
			Popup.Closed -= base.CardPopupClosed;
			Popup.Closed += base.CardPopupClosed;
		}
	}

	protected override void Init(CardViewModel model)
	{
		SelectedTypes = ((TaskTypeViewModel)model).Values;
	}

	protected override ConditionEditDialog GenerateEditDialog()
	{
		return new TaskTypeEditDialog(SelectedTypes);
	}

	protected override bool CheckFilterValid()
	{
		return SelectedTypes.Count > 0;
	}
}
