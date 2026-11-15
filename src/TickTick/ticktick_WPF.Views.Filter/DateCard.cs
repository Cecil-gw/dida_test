using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class DateCard : CardBase
{
	private List<string> SelectedDates { get; set; } = new List<string>();

	public DateCard(CardViewModel viewModel)
		: base(viewModel)
	{
		if (EditDialog is DateEditDialog dateEditDialog)
		{
			dateEditDialog.OnSelectedDateChanged += delegate(object sender, List<string> dates)
			{
				SelectedDates = dates;
				((DateCardViewModel)ViewModel).Values = dates;
				ViewModel.Content = ViewModel.ToCardText();
			};
			dateEditDialog.OnLogicChanged += delegate(object sender, LogicType logicType)
			{
				((DateCardViewModel)ViewModel).LogicType = logicType;
				ViewModel.Content = ViewModel.ToCardText();
			};
			dateEditDialog.OnCancel += delegate
			{
				Popup.IsOpen = false;
			};
			dateEditDialog.OnSave += delegate
			{
				IsSave = true;
				Popup.IsOpen = false;
			};
			Popup.Closed -= base.CardPopupClosed;
			Popup.Closed += base.CardPopupClosed;
		}
	}

	protected override bool CheckFilterValid()
	{
		return SelectedDates.Count > 0;
	}

	protected override void Init(CardViewModel model)
	{
		SelectedDates = ((DateCardViewModel)model).Values;
	}

	protected override ConditionEditDialog GenerateEditDialog()
	{
		return new DateEditDialog(showAll: false, SelectedDates, ((DateCardViewModel)ViewModel).LogicType, ViewModel.Version);
	}
}
