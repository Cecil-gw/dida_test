using System.Collections.Generic;

namespace ticktick_WPF.ViewModels;

public static class FilterConditionProvider
{
	public static List<CardViewModel> BuildInitData()
	{
		return new List<CardViewModel>
		{
			new CardViewModel
			{
				Type = CardType.InitFilter
			},
			new CardViewModel
			{
				Type = CardType.InitLogic
			},
			new CardViewModel
			{
				Type = CardType.InitFilter
			},
			new CardViewModel
			{
				Type = CardType.AddMore
			}
		};
	}

	public static List<CardViewModel> AddMoreCondition(List<CardViewModel> conditions)
	{
		conditions.ForEach(delegate(CardViewModel con)
		{
			con.ShowDropdown = false;
		});
		CardViewModel cardViewModel = conditions.Find((CardViewModel x) => x.Type == CardType.AddMore);
		if (cardViewModel != null)
		{
			conditions.Remove(cardViewModel);
		}
		conditions.Add(new CardViewModel
		{
			Type = CardType.InitLogic
		});
		conditions.Add(new CardViewModel
		{
			Type = CardType.InitFilter
		});
		if (conditions.Count <= 3)
		{
			conditions.Add(new CardViewModel
			{
				Type = CardType.AddMore
			});
		}
		return conditions;
	}
}
