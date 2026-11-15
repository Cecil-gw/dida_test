using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class LogicConditionCard : ConditionCard
{
	public LogicConditionCard(CardViewModel viewModel)
		: base(viewModel)
	{
	}

	protected override void OnCardClick()
	{
		ShowSelectLogicDialog();
	}

	protected override void OnDeleteClick()
	{
		ViewModel.Type = CardType.InitLogic;
	}
}
