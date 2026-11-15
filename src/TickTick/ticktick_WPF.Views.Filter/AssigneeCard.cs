using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class AssigneeCard : CardBase
{
	private List<string> SelectedAssignee { get; set; } = new List<string>();

	public AssigneeCard(CardViewModel viewModel)
		: base(viewModel)
	{
		if (EditDialog is AssigneeEditDialog assigneeEditDialog)
		{
			assigneeEditDialog.OnSelectedAssigneeChanged += delegate(object sender, List<string> assignees)
			{
				SelectedAssignee = assignees;
				((AssigneeCardViewModel)ViewModel).Values = assignees;
				ViewModel.Content = ViewModel.ToCardText();
			};
			assigneeEditDialog.OnCancel += delegate
			{
				Popup.IsOpen = false;
			};
			assigneeEditDialog.OnSave += delegate
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
		SelectedAssignee = ((AssigneeCardViewModel)model).Values;
	}

	protected override ConditionEditDialog GenerateEditDialog()
	{
		return new AssigneeEditDialog(SelectedAssignee, showAll: false);
	}

	protected override bool CheckFilterValid()
	{
		return SelectedAssignee.Count > 0;
	}
}
