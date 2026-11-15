using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class PriorityCard : CardBase
{
	private List<int> SelectedPriority { get; set; } = new List<int>();

	public PriorityCard(CardViewModel viewModel)
		: base(viewModel)
	{
		if (EditDialog is PriorityEditDialog priorityEditDialog)
		{
			priorityEditDialog.OnSelectedPriorityChanged += delegate(object sender, List<int> priorities)
			{
				SelectedPriority = priorities;
				((PriorityCardViewModel)ViewModel).Values = priorities;
				ViewModel.Content = ViewModel.ToCardText();
			};
			priorityEditDialog.OnCancel += delegate
			{
				Popup.IsOpen = false;
			};
			priorityEditDialog.OnSave += delegate
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
		SelectedPriority = ((PriorityCardViewModel)model).Values;
	}

	protected override ConditionEditDialog GenerateEditDialog()
	{
		return new PriorityEditDialog(SelectedPriority);
	}

	protected override bool CheckFilterValid()
	{
		return SelectedPriority.Count > 0;
	}
}
