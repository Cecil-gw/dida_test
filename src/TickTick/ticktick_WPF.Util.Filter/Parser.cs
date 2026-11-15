using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.Util.Filter;

public static class Parser
{
	public static int GetFilterRuleType(string rule)
	{
		if (string.IsNullOrEmpty(rule))
		{
			return 0;
		}
		try
		{
			return JsonConvert.DeserializeObject<RuleModel>(rule).type;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	public static AdvancedFilterViewModel ToAdvanceModel(string rule)
	{
		AdvancedFilterViewModel advancedFilterViewModel = new AdvancedFilterViewModel();
		if (string.IsNullOrEmpty(rule))
		{
			advancedFilterViewModel.CardList = FilterConditionProvider.BuildInitData();
		}
		else
		{
			List<CardViewModel> list = new List<CardViewModel>();
			List<CardViewModel> list2 = new List<CardViewModel>();
			AdvancedRuleModel advancedRuleModel;
			try
			{
				advancedRuleModel = JsonConvert.DeserializeObject<AdvancedRuleModel>(rule);
			}
			catch (Exception)
			{
				advancedRuleModel = new AdvancedRuleModel();
			}
			if (advancedRuleModel == null)
			{
				advancedRuleModel = new AdvancedRuleModel();
			}
			if (advancedRuleModel.and != null)
			{
				list2.Add(new CardViewModel
				{
					Type = CardType.LogicAnd
				});
				ParseModelList(advancedRuleModel.and, list, list2, advancedRuleModel.version);
			}
			else if (advancedRuleModel.or != null)
			{
				list2.Add(new CardViewModel
				{
					Type = CardType.LogicOr
				});
				ParseModelList(advancedRuleModel.or, list, list2, advancedRuleModel.version);
			}
			advancedFilterViewModel.Version = advancedRuleModel.version;
			switch (list.Count)
			{
			case 1:
				advancedFilterViewModel.CardList.AddRange(list);
				advancedFilterViewModel.CardList.Add(new CardViewModel
				{
					Type = CardType.InitLogic
				});
				advancedFilterViewModel.CardList.Add(new CardViewModel
				{
					Type = CardType.InitFilter
				});
				advancedFilterViewModel.CardList.Add(new CardViewModel
				{
					Type = CardType.AddMore
				});
				break;
			case 2:
				advancedFilterViewModel.CardList.Add(list[0]);
				advancedFilterViewModel.CardList.Add(list2[0]);
				advancedFilterViewModel.CardList.Add(list[1]);
				advancedFilterViewModel.CardList.Add(new CardViewModel
				{
					Type = CardType.AddMore
				});
				break;
			case 3:
			{
				CardViewModel item = new CardViewModel
				{
					Type = list2[0].Type
				};
				CardViewModel item2 = new CardViewModel
				{
					Type = list2[0].Type
				};
				if (list2.Count > 1)
				{
					item = new CardViewModel
					{
						Type = list2[1].Type
					};
				}
				advancedFilterViewModel.CardList.Add(list[0]);
				advancedFilterViewModel.CardList.Add(item);
				advancedFilterViewModel.CardList.Add(list[1]);
				advancedFilterViewModel.CardList.Add(item2);
				advancedFilterViewModel.CardList.Add(list[2]);
				break;
			}
			}
		}
		return advancedFilterViewModel;
	}

	private static void ParseModelList(List<object> modelList, List<CardViewModel> conditionList, List<CardViewModel> logicList, int version)
	{
		if (modelList == null)
		{
			return;
		}
		foreach (object model in modelList)
		{
			CardViewModel cardViewModel = null;
			ListRuleModel listRuleModel;
			try
			{
				listRuleModel = JsonConvert.DeserializeObject<ListRuleModel>(model.ToString());
			}
			catch (Exception)
			{
				listRuleModel = new ListRuleModel();
			}
			if (listRuleModel.conditionName == null)
			{
				if (listRuleModel.or != null)
				{
					logicList.Add(new CardViewModel
					{
						Type = CardType.LogicOr
					});
					ParseModelList(listRuleModel.or, conditionList, logicList, version);
				}
				else
				{
					logicList.Add(new CardViewModel
					{
						Type = CardType.LogicAnd
					});
					ParseModelList(listRuleModel.and, conditionList, logicList, version);
				}
				continue;
			}
			LogicType logicType = LogicType.Or;
			if (listRuleModel.or != null)
			{
				logicType = LogicType.Or;
			}
			else if (listRuleModel.and != null)
			{
				logicType = LogicType.And;
			}
			else if (listRuleModel.not != null)
			{
				logicType = LogicType.Not;
			}
			List<object> list = (listRuleModel.or ?? listRuleModel.and) ?? listRuleModel.not;
			switch (listRuleModel.conditionName)
			{
			case "tag":
			{
				TagCardViewModel tagCardViewModel = new TagCardViewModel
				{
					LogicType = logicType
				};
				List<string> list2 = new List<string>();
				List<string> list3 = TagSerializer.NormalizeNames(GetStringList(list));
				List<TagModel> tags = CacheManager.GetTags();
				foreach (string item in list3)
				{
					if (!list2.Contains(item))
					{
						list2.Add(item);
					}
					string parent = item;
					List<string> collection = tags.Where((TagModel t) => t.parent == parent)?.Select((TagModel t) => t.name).ToList();
					list2.AddRange(collection);
				}
				tagCardViewModel.Values = list2;
				cardViewModel = tagCardViewModel;
				break;
			}
			case "dueDate":
				cardViewModel = new DateCardViewModel
				{
					LogicType = logicType,
					Values = GetStringList(list),
					Version = version
				};
				break;
			case "assignee":
				cardViewModel = new AssigneeCardViewModel
				{
					LogicType = logicType,
					Values = GetStringList(list),
					Version = version
				};
				break;
			case "priority":
				cardViewModel = new PriorityCardViewModel
				{
					LogicType = logicType,
					Values = GetIntList(list)
				};
				break;
			case "taskType":
				cardViewModel = new TaskTypeViewModel
				{
					LogicType = logicType,
					Values = GetStringList(list)
				};
				break;
			case "keywords":
			{
				List<string> stringList = GetStringList(list);
				cardViewModel = new KeywordsViewModel
				{
					LogicType = logicType,
					Keyword = ((stringList != null && stringList.Count > 0) ? stringList[0] : string.Empty)
				};
				break;
			}
			case "listOrGroup":
				cardViewModel = new ProjectOrGroupCardViewModel();
				((ProjectOrGroupCardViewModel)cardViewModel).LogicType = logicType;
				if (list == null || list.Count <= 0)
				{
					break;
				}
				foreach (object item2 in list)
				{
					ListRuleModel listRuleModel2;
					try
					{
						listRuleModel2 = JsonConvert.DeserializeObject<ListRuleModel>(item2.ToString());
					}
					catch (Exception)
					{
						listRuleModel2 = new ListRuleModel();
					}
					switch (listRuleModel2.conditionName)
					{
					case "list":
						if (listRuleModel2.or != null && listRuleModel2.or.Any())
						{
							AddUniqueItems(((ProjectOrGroupCardViewModel)cardViewModel).Values, GetStringList(listRuleModel2.or));
						}
						break;
					case "group":
						if (listRuleModel2.or != null && listRuleModel2.or.Any())
						{
							AddUniqueItems(((ProjectOrGroupCardViewModel)cardViewModel).GroupIds, GetStringList(listRuleModel2.or));
						}
						break;
					case "account":
					case "bind_account":
						if (listRuleModel2.or != null && listRuleModel2.or.Any())
						{
							ProjectOrGroupCardViewModel projectOrGroupCardViewModel2 = (ProjectOrGroupCardViewModel)cardViewModel;
							AddUniqueItems(projectOrGroupCardViewModel2.CalendarAccounts, GetStringList(listRuleModel2.or));
							if (!projectOrGroupCardViewModel2.Values.Contains("Calendar5959a2259161d16d23a4f272"))
							{
								projectOrGroupCardViewModel2.Values.Add("Calendar5959a2259161d16d23a4f272");
							}
						}
						break;
					case "calendar":
					case "subscribe_calendar":
						if (listRuleModel2.or != null && listRuleModel2.or.Any())
						{
							ProjectOrGroupCardViewModel projectOrGroupCardViewModel = (ProjectOrGroupCardViewModel)cardViewModel;
							AddUniqueItems(projectOrGroupCardViewModel.Calendars, GetStringList(listRuleModel2.or));
							if (!projectOrGroupCardViewModel.Values.Contains("Calendar5959a2259161d16d23a4f272"))
							{
								projectOrGroupCardViewModel.Values.Add("Calendar5959a2259161d16d23a4f272");
							}
						}
						break;
					}
				}
				break;
			}
			if (cardViewModel != null)
			{
				conditionList.Add(cardViewModel);
			}
		}
	}

	private static List<string> GetStringList(List<object> values)
	{
		List<string> list = new List<string>();
		if (values != null && values.Any())
		{
			foreach (object value in values)
			{
				if (value != null)
				{
					list.Add(value.ToString());
				}
			}
		}
		return list;
	}

	private static List<int> GetIntList(List<object> values)
	{
		List<int> list = new List<int>();
		if (values != null && values.Any())
		{
			foreach (object value in values)
			{
				list.Add(int.Parse(value.ToString()));
			}
		}
		return list;
	}

	public static NormalFilterViewModel ToNormalModel(string rule)
	{
		if (string.IsNullOrEmpty(rule))
		{
			return new NormalFilterViewModel();
		}
		if (!rule.StartsWith("{") && !rule.EndsWith("}"))
		{
			return ParseLegacyRule(rule);
		}
		NormalFilterViewModel normalFilterViewModel = new NormalFilterViewModel();
		NormalRuleModel normalRuleModel;
		try
		{
			normalRuleModel = JsonConvert.DeserializeObject<NormalRuleModel>(rule);
		}
		catch (Exception)
		{
			normalRuleModel = new NormalRuleModel();
		}
		if (normalRuleModel.and == null || normalRuleModel.and.Count == 0)
		{
			return normalFilterViewModel;
		}
		normalFilterViewModel.Version = normalRuleModel.version;
		foreach (object item in normalRuleModel.and)
		{
			if (item == null)
			{
				continue;
			}
			string text = item.ToString();
			if (text.Contains("keywords") || (!text.Contains("priority") && !text.Contains("listOrGroup")))
			{
				StringRule ruleModelSafely = GetRuleModelSafely<StringRule>(text);
				switch (ruleModelSafely.conditionName)
				{
				case "assignee":
					normalFilterViewModel.Assignees = ruleModelSafely.or;
					break;
				case "tag":
					normalFilterViewModel.Tags = ruleModelSafely.or;
					break;
				case "dueDate":
					normalFilterViewModel.DueDates = ruleModelSafely.or;
					break;
				case "keywords":
					normalFilterViewModel.Keywords = ruleModelSafely.or;
					break;
				case "taskType":
					normalFilterViewModel.TaskTypes = ruleModelSafely.or;
					break;
				case "status":
					normalFilterViewModel.Status = ruleModelSafely.or;
					break;
				}
			}
			if (text.Contains("priority"))
			{
				IntRule ruleModelSafely2 = GetRuleModelSafely<IntRule>(text);
				normalFilterViewModel.Priorities = ruleModelSafely2.or;
			}
			if (text.Contains("date"))
			{
				ListOrGroupRule ruleModelSafely3 = GetRuleModelSafely<ListOrGroupRule>(text);
				if (ruleModelSafely3.or == null || ruleModelSafely3.or.Count == 0)
				{
					continue;
				}
				foreach (object item2 in ruleModelSafely3.or)
				{
					if (item2.ToString().Contains("dueDate"))
					{
						StringRule ruleModelSafely4 = GetRuleModelSafely<StringRule>(item2.ToString());
						normalFilterViewModel.DueDates = ruleModelSafely4.or;
					}
					else if (item2.ToString().Contains("completedTime"))
					{
						StringRule ruleModelSafely5 = GetRuleModelSafely<StringRule>(item2.ToString());
						normalFilterViewModel.CompletedTimes = ruleModelSafely5.or;
					}
				}
			}
			if (!text.Contains("listOrGroup"))
			{
				continue;
			}
			ListOrGroupRule ruleModelSafely6 = GetRuleModelSafely<ListOrGroupRule>(text);
			if (ruleModelSafely6.or == null || ruleModelSafely6.or.Count == 0)
			{
				continue;
			}
			foreach (object item3 in ruleModelSafely6.or)
			{
				StringRule ruleModelSafely7 = GetRuleModelSafely<StringRule>(item3.ToString());
				switch (ruleModelSafely7.conditionName)
				{
				case "list":
					AddUniqueItems(normalFilterViewModel.Projects, ruleModelSafely7.or);
					break;
				case "group":
					AddUniqueItems(normalFilterViewModel.Groups, ruleModelSafely7.or);
					break;
				case "account":
				case "bind_account":
					AddUniqueItems(normalFilterViewModel.CalendarAccounts, ruleModelSafely7.or);
					break;
				case "calendar":
				case "subscribe_calendar":
					AddUniqueItems(normalFilterViewModel.Calendars, ruleModelSafely7.or);
					break;
				}
			}
		}
		if ((normalFilterViewModel.CalendarAccounts.Any() || normalFilterViewModel.Calendars.Any()) && !normalFilterViewModel.Projects.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			normalFilterViewModel.Projects.Add("Calendar5959a2259161d16d23a4f272");
		}
		return normalFilterViewModel;
	}

	private static void AddUniqueItems(List<string> target, IEnumerable<string> items)
	{
		if (target == null || items == null)
		{
			return;
		}
		foreach (string item in items.Where((string item) => !string.IsNullOrEmpty(item)))
		{
			if (!target.Contains(item))
			{
				target.Add(item);
			}
		}
	}

	private static NormalFilterViewModel ParseLegacyRule(string rule)
	{
		NormalFilterViewModel normalFilterViewModel = new NormalFilterViewModel();
		string[] array = Regex.Split(rule, "AND");
		if (array.Length == 0)
		{
			return normalFilterViewModel;
		}
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text = array2[i].Trim();
			if (text.StartsWith("tag"))
			{
				normalFilterViewModel.Tags = ParseItems(text, "tag");
			}
			else if (text.StartsWith("dueDate"))
			{
				normalFilterViewModel.DueDates = ParseItems(text, "dueDate");
			}
			else if (text.StartsWith("assignee"))
			{
				normalFilterViewModel.Assignees = ParseItems(text, "assignee");
			}
			else if (text.StartsWith("list"))
			{
				normalFilterViewModel.Projects = ParseItems(text, "list");
			}
			else if (text.StartsWith("group"))
			{
				normalFilterViewModel.Groups = ParseItems(text, "group");
			}
			else if (text.StartsWith("priority"))
			{
				normalFilterViewModel.Priorities = ParsePriorities(text);
			}
		}
		return normalFilterViewModel;
	}

	private static List<int> ParsePriorities(string filter)
	{
		try
		{
			return filter.Substring("priority".Length + 1).Split(',').Select(int.Parse)
				.ToList();
		}
		catch
		{
			return new List<int>();
		}
	}

	private static List<string> ParseItems(string filter, string category)
	{
		if (category.Length + 1 < filter.Length)
		{
			return (from item in filter.Substring(category.Length + 1).Split(',')
				select item.Trim()).ToList();
		}
		return new List<string>();
	}

	private static T GetRuleModelSafely<T>(string rule) where T : new()
	{
		try
		{
			return JsonConvert.DeserializeObject<T>(rule);
		}
		catch
		{
			return new T();
		}
	}

	public static bool CheckFilterRuleExpired(string rule)
	{
		if (GetFilterRuleType(rule) == 0)
		{
			NormalFilterViewModel normalFilterViewModel = ToNormalModel(rule);
			if (normalFilterViewModel.Projects.Count > 0 || normalFilterViewModel.Groups.Count > 0)
			{
				return !CheckProjectOrGroupValid(normalFilterViewModel.Projects, normalFilterViewModel.Groups);
			}
			if (normalFilterViewModel.Tags.Count <= 0)
			{
				return false;
			}
			if (!CheckTagValid(normalFilterViewModel.Tags))
			{
				return true;
			}
			return false;
		}
		AdvancedFilterViewModel advancedFilterViewModel = ToAdvanceModel(rule);
		CardViewModel cardViewModel = advancedFilterViewModel?.CardList.FirstOrDefault((CardViewModel card) => card.ConditionName == "tag");
		if (cardViewModel != null && !CheckTagValid(((TagCardViewModel)cardViewModel).Values))
		{
			return true;
		}
		if (!(advancedFilterViewModel?.CardList.FirstOrDefault((CardViewModel card) => card.ConditionName == "listOrGroup") is ProjectOrGroupCardViewModel projectOrGroupCardViewModel))
		{
			return false;
		}
		return !CheckProjectOrGroupValid(projectOrGroupCardViewModel.Values, projectOrGroupCardViewModel.GroupIds);
	}

	public static bool CheckProjectOrGroupValid(List<string> projectIds, List<string> groupIds)
	{
		if (projectIds.Count <= 0 && groupIds.Count <= 0)
		{
			return true;
		}
		if (projectIds.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			return true;
		}
		if (CalendarUtils.ContainsAllListId(projectIds))
		{
			return true;
		}
		return CacheManager.GetProjects().Count((ProjectModel p) => (projectIds.Contains(p.id) || groupIds.Contains(p.groupId)) && p.IsValid()) != 0;
	}

	private static bool CheckTagValid(IEnumerable<string> tags)
	{
		List<string> cachedTags = (from t in TagDataHelper.GetTags(sort: false)
			select t.name).ToList();
		cachedTags.Add("*withtags");
		cachedTags.Add("!tag");
		cachedTags.Add("*tag");
		return tags.All((string tag) => cachedTags.Contains(tag));
	}

	public static int GetFilterRuleVersion(string rule)
	{
		if (string.IsNullOrEmpty(rule))
		{
			return 1;
		}
		try
		{
			return JsonConvert.DeserializeObject<RuleModel>(rule)?.version ?? 1;
		}
		catch (Exception)
		{
			return 1;
		}
	}

	public static bool ContainsDate(string rule, bool onlyNormal = false)
	{
		if (onlyNormal || GetFilterRuleType(rule) == 0)
		{
			NormalFilterViewModel normalFilterViewModel = ToNormalModel(rule);
			if (normalFilterViewModel.DueDates != null)
			{
				return normalFilterViewModel.DueDates.Any((string d) => d != "nodue" && d != "recurring");
			}
			return false;
		}
		return ToAdvanceModel(rule).CardList.Where((CardViewModel card) => card.Type == CardType.Filter).ToList().Any((CardViewModel c) => c is DateCardViewModel { Values: not null } dateCardViewModel && dateCardViewModel.Values.Any((string v) => v != "nodue" && v != "recurring"));
	}
}
