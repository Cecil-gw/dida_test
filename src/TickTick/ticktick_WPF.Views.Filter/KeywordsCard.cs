using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class KeywordsCard : ConditionCard
{
	public KeywordsCard(CardViewModel viewModel)
		: base(viewModel)
	{
	}

	protected override void OnDeleteClick()
	{
		FireEvent(new ChangeData
		{
			Type = ChangeType.Filter,
			From = ViewModel,
			To = new CardViewModel
			{
				Type = CardType.InitFilter
			}
		});
	}
}
