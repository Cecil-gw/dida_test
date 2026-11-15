using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class AddMoreCard : ConditionCard
{
	public AddMoreCard(CardViewModel viewModel)
		: base(viewModel)
	{
	}

	protected override void OnCardClick()
	{
		FireEvent(new ChangeData
		{
			Type = ChangeType.AddMore
		});
	}
}
