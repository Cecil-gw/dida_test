using ticktick_WPF.ViewModels;

namespace ticktick_WPF.Views.Filter;

public class ChangeData
{
	public ChangeType Type { get; set; }

	public CardViewModel From { get; set; }

	public CardViewModel To { get; set; }
}
