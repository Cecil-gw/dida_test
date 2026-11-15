using System.Collections.Generic;
using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class TagCard : CardBase
{
	private List<string> SelectedTags { get; set; } = new List<string>();

	private LogicType LogicType { get; set; } = LogicType.Or;

	public TagCard(CardViewModel viewModel)
		: base(viewModel)
	{
		InitEditDialog();
	}

	private void InitEditDialog()
	{
		if (EditDialog is TagEditDialog tagEditDialog)
		{
			tagEditDialog.OnSelectedTagChanged += delegate(object sender, List<string> tags)
			{
				SelectedTags = tags;
				((TagCardViewModel)ViewModel).Values = tags;
				ViewModel.Content = ViewModel.ToCardText();
			};
			tagEditDialog.OnLogicChanged += delegate(object sender, LogicType logicType)
			{
				LogicType = logicType;
				((TagCardViewModel)ViewModel).LogicType = LogicType;
				ViewModel.Content = ViewModel.ToCardText();
			};
			tagEditDialog.OnCancel += delegate
			{
				Popup.IsOpen = false;
			};
			tagEditDialog.OnSave += delegate
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
		SelectedTags = ((TagCardViewModel)model).Values;
		LogicType = ((TagCardViewModel)model).LogicType;
	}

	protected override ConditionEditDialog GenerateEditDialog()
	{
		return new TagEditDialog(showAll: false, SelectedTags, LogicType);
	}

	protected override bool CheckFilterValid()
	{
		return SelectedTags.Count > 0;
	}
}
