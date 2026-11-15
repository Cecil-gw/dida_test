using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;

namespace ticktick_WPF.ViewModels;

public class AdvancedFilterViewModel : BaseViewModel
{
	public int Version = 1;

	public List<CardViewModel> CardList { get; set; } = new List<CardViewModel>();

	public string ToRule(bool isSave)
	{
		List<CardViewModel> list = CardList.Where((CardViewModel x) => x.Type == CardType.Filter || x.Type == CardType.LogicAnd || x.Type == CardType.LogicOr).ToList();
		int num = ((!WithNotLogic(list)) ? 1 : 2);
		foreach (CardViewModel item in list)
		{
			string text = null;
			switch (item.ConditionName)
			{
			case "listOrGroup":
				text = "project";
				if (item is ProjectOrGroupCardViewModel projectOrGroupCardViewModel && projectOrGroupCardViewModel.RequiresVersion7())
				{
					num = Math.Max(7, num);
				}
				break;
			case "tag":
				text = "tag";
				break;
			case "priority":
				text = "priority";
				break;
			case "keywords":
				text = "content";
				num = Math.Max(3, num);
				break;
			case "taskType":
				text = "type";
				num = Math.Max(3, num);
				break;
			case "dueDate":
			{
				text = "date";
				Rule<string> rule2 = (item as FilterListDisplayModel<string>)?.ToRule();
				List<string> list2 = rule2?.or ?? rule2?.not;
				if (list2 != null)
				{
					if (list2.Any((string d) => d.StartsWith("offset")))
					{
						num = Math.Max(6, num);
					}
					else if (list2.Any((string d) => d.StartsWith("span")))
					{
						num = Math.Max(4, num);
					}
					else if (list2.Any((string date) => date.EndsWith("daysfromtoday") || date == "recurring"))
					{
						num = Math.Max(3, num);
					}
				}
				break;
			}
			case "assignee":
			{
				text = "assignee";
				Rule<string> rule = (item as FilterListDisplayModel<string>)?.ToRule();
				if (rule?.or != null && rule.or.Any((string assign) => assign != "me" && assign != "other" && assign != "noassignee"))
				{
					num = Math.Max(3, num);
				}
				break;
			}
			}
			if (isSave && !string.IsNullOrEmpty(text))
			{
				UserActCollectUtils.AddClickEvent("edit_filter", "conditions", text);
			}
		}
		switch (list.Count)
		{
		case 1:
		{
			AdvancedRuleModel advancedRuleModel2 = new AdvancedRuleModel
			{
				and = new List<object>()
			};
			if (!AddRule(list[0], advancedRuleModel2, isAnd: true))
			{
				return string.Empty;
			}
			advancedRuleModel2.version = num;
			return JsonConvert.SerializeObject(advancedRuleModel2);
		}
		case 3:
		{
			AdvancedRuleModel advancedRuleModel3 = new AdvancedRuleModel();
			if (list[1].Type == CardType.LogicAnd)
			{
				advancedRuleModel3.and = new List<object>();
				AddRule(list[0], advancedRuleModel3, isAnd: true);
				AddRule(list[2], advancedRuleModel3, isAnd: true);
			}
			else
			{
				advancedRuleModel3.or = new List<object>();
				AddRule(list[0], advancedRuleModel3, isAnd: false);
				AddRule(list[2], advancedRuleModel3, isAnd: false);
			}
			if (!HasAnyRule(advancedRuleModel3))
			{
				return string.Empty;
			}
			advancedRuleModel3.version = num;
			return JsonConvert.SerializeObject(advancedRuleModel3);
		}
		case 5:
		{
			AdvancedRuleModel advancedRuleModel = new AdvancedRuleModel();
			if (IsSameLogic(list[1], list[3]))
			{
				if (list[1].Type == CardType.LogicAnd || 1 == 0)
				{
					advancedRuleModel.and = new List<object>();
					AddRule(list[0], advancedRuleModel, isAnd: true);
					AddRule(list[2], advancedRuleModel, isAnd: true);
					AddRule(list[4], advancedRuleModel, isAnd: true);
				}
				else
				{
					advancedRuleModel.or = new List<object>();
					AddRule(list[0], advancedRuleModel, isAnd: false);
					AddRule(list[2], advancedRuleModel, isAnd: false);
					AddRule(list[4], advancedRuleModel, isAnd: false);
				}
			}
			else
			{
				bool num2 = list[1].Type != CardType.LogicAnd;
				LogicType logicType = ((list[3].Type != CardType.LogicAnd) ? LogicType.Or : LogicType.And);
				BaseRuleModel baseRuleModel = new BaseRuleModel();
				if (!num2)
				{
					baseRuleModel.and = new List<object>();
					AddRule(list[0], ref baseRuleModel.and);
					AddRule(list[2], ref baseRuleModel.and);
				}
				else
				{
					baseRuleModel.or = new List<object>();
					AddRule(list[0], ref baseRuleModel.or);
					AddRule(list[2], ref baseRuleModel.or);
				}
				if (logicType == LogicType.And)
				{
					advancedRuleModel.and = new List<object>();
					if (HasAnyRule(baseRuleModel))
					{
						advancedRuleModel.and.Add(baseRuleModel);
					}
					AddRule(list[4], ref advancedRuleModel.and);
				}
				else
				{
					advancedRuleModel.or = new List<object>();
					if (HasAnyRule(baseRuleModel))
					{
						advancedRuleModel.or.Add(baseRuleModel);
					}
					AddRule(list[4], ref advancedRuleModel.or);
				}
			}
			if (!HasAnyRule(advancedRuleModel))
			{
				return string.Empty;
			}
			advancedRuleModel.version = num;
			return JsonConvert.SerializeObject(advancedRuleModel);
		}
		default:
			return string.Empty;
		}
	}

	private bool WithNotLogic(List<CardViewModel> valid)
	{
		if (valid.Any())
		{
			foreach (CardViewModel item in valid)
			{
				if (item.LogicType == LogicType.Not)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsSameLogic(CardViewModel left, CardViewModel right)
	{
		if (left.Type != CardType.LogicAnd || right.Type != CardType.LogicAnd)
		{
			if (left.Type == CardType.LogicOr)
			{
				return right.Type == CardType.LogicOr;
			}
			return false;
		}
		return true;
	}

	private static bool HasAnyRule(AdvancedRuleModel model)
	{
		List<object> and = model.and;
		if (and == null || !and.Any())
		{
			return model.or?.Any() ?? false;
		}
		return true;
	}

	private static bool HasAnyRule(BaseRuleModel model)
	{
		List<object> and = model.and;
		if (and == null || !and.Any())
		{
			return model.or?.Any() ?? false;
		}
		return true;
	}

	private static object BuildRule(CardViewModel card)
	{
		if (card.ConditionName == "priority")
		{
			return (card as FilterListDisplayModel<int>)?.ToRule();
		}
		return (card as FilterListDisplayModel<string>)?.ToRule();
	}

	private static bool AddRule(CardViewModel card, ref List<object> condList)
	{
		object obj = BuildRule(card);
		if (obj == null)
		{
			return false;
		}
		condList.Add(obj);
		return true;
	}

	private static bool AddRule(CardViewModel card, AdvancedRuleModel advanced, bool isAnd)
	{
		object obj = BuildRule(card);
		if (obj == null)
		{
			return false;
		}
		if (isAnd)
		{
			advanced.and.Add(obj);
		}
		else
		{
			advanced.or.Add(obj);
		}
		return true;
	}

	public bool ConditionValid()
	{
		List<CardViewModel> list = CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList();
		List<CardViewModel> list2 = CardList.Where((CardViewModel card) => card.Type == CardType.LogicAnd || card.Type == CardType.LogicOr).ToList();
		if ((list.Count != 1 || list2.Count != 0) && (list.Count != 2 || list2.Count != 1))
		{
			if (list.Count == 3)
			{
				return list2.Count == 2;
			}
			return false;
		}
		return true;
	}

	public bool CheckKeywordValid()
	{
		CardViewModel cardViewModel = CardList.FirstOrDefault((CardViewModel card) => card is KeywordsViewModel);
		if (cardViewModel != null && cardViewModel is KeywordsViewModel keywordsViewModel)
		{
			return !string.IsNullOrEmpty(keywordsViewModel.Keyword);
		}
		return true;
	}

	public bool OnlyNote()
	{
		if (CardList == null || !CardList.Any())
		{
			return false;
		}
		if (CardList.Any((CardViewModel card) => card.Type == CardType.LogicOr))
		{
			return false;
		}
		foreach (CardViewModel card in CardList)
		{
			if (card is TaskTypeViewModel taskTypeViewModel)
			{
				return taskTypeViewModel.IsNote;
			}
		}
		return false;
	}

	public bool FilterEvent()
	{
		foreach (CardViewModel card in CardList)
		{
			if (card is ProjectOrGroupCardViewModel projectOrGroupCardViewModel)
			{
				bool flag = projectOrGroupCardViewModel.Values.Contains("Calendar5959a2259161d16d23a4f272");
				if (projectOrGroupCardViewModel.LogicType != LogicType.Not)
				{
					return flag;
				}
				bool flag2 = projectOrGroupCardViewModel.CalendarAccounts.Any((string id) => !string.IsNullOrEmpty(id)) || projectOrGroupCardViewModel.Calendars.Any((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe");
				if (flag && !flag2)
				{
					return false;
				}
				return flag2 || projectOrGroupCardViewModel.Values.Any(CalendarUtils.IsAllListId);
			}
		}
		return false;
	}
}
