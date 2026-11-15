using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Cache;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Filter;
using ticktick_WPF.Views.Project;

namespace ticktick_WPF.ViewModels;

public class ProjectOrGroupCardViewModel : FilterListDisplayModel<string>
{
	public List<string> Values
	{
		get
		{
			return ValueList;
		}
		set
		{
			ValueList = value;
			OnPropertyChanged("Values");
		}
	}

	public List<string> GroupIds { get; set; } = new List<string>();

	public List<string> CalendarAccounts { get; set; } = new List<string>();

	public List<string> Calendars { get; set; } = new List<string>();

	public ProjectOrGroupCardViewModel()
	{
		base.ConditionName = "listOrGroup";
	}

	public override ConditionEditDialog GenerateEditDialog()
	{
		return new ProjectOrGroupEditDialog();
	}

	public override Rule<string> ToRule()
	{
		List<ProjectModel> projects = CacheManager.GetProjects();
		ListOrGroupRule listOrGroupRule = null;
		if (ValueList.Count > 0 || GroupIds.Count > 0 || CalendarAccounts.Count > 0 || Calendars.Count > 0)
		{
			List<string> list = new List<string>();
			List<string> projectIds = ValueList.Where((string p) => !string.IsNullOrEmpty(p)).ToList();
			List<string> groupIds = GroupIds.Where((string g) => !string.IsNullOrEmpty(g)).ToList();
			List<string> list2 = CalendarAccounts.Where((string id) => !string.IsNullOrEmpty(id)).ToList();
			List<string> list3 = Calendars.Where((string id) => !string.IsNullOrEmpty(id)).ToList();
			bool flag = projectIds.RemoveAll(CalendarUtils.IsAllListId) > 0;
			bool num = list3.RemoveAll((string id) => id == "#allSubscribe") > 0;
			CalendarFilterRuleNormalizer.MoveAccountIdsFromCalendars(list2, list3);
			bool flag2 = num || (projectIds.Contains("Calendar5959a2259161d16d23a4f272") && list2.Count == 0 && list3.Count == 0);
			if (flag2)
			{
				projectIds.RemoveAll((string id) => id == "Calendar5959a2259161d16d23a4f272");
			}
			bool flag3 = base.LogicType != LogicType.Not && flag && flag2 && groupIds.Count == 0 && projectIds.Count == 0;
			if (!flag3 && flag && !projectIds.Contains("ProjectAll2e4c103c57ef480997943206"))
			{
				projectIds.Insert(0, "ProjectAll2e4c103c57ef480997943206");
			}
			if (flag2)
			{
				list2.Clear();
				list3.Clear();
				if (!flag3)
				{
					projectIds.Add("Calendar5959a2259161d16d23a4f272");
				}
			}
			else if (list2.Count > 0 || list3.Count > 0)
			{
				projectIds.RemoveAll((string id) => id == "Calendar5959a2259161d16d23a4f272");
			}
			if (projectIds.Count == 0 && groupIds.Count == 0 && list2.Count == 0 && list3.Count == 0)
			{
				return null;
			}
			bool num2 = projectIds.Contains("ProjectAll2e4c103c57ef480997943206");
			bool flag4 = projectIds.Contains("Calendar5959a2259161d16d23a4f272");
			if (groupIds.Count > 0)
			{
				List<ProjectModel> source = projects.Where((ProjectModel p) => projectIds.Contains(p.id)).ToList();
				list.AddRange(from project in source
					where string.IsNullOrEmpty(project.groupId) || project.groupId == "NONE" || !groupIds.Contains(project.groupId)
					select project.id);
			}
			else
			{
				list = projectIds;
			}
			if (num2 && !list.Contains("ProjectAll2e4c103c57ef480997943206"))
			{
				list.Insert(0, "ProjectAll2e4c103c57ef480997943206");
			}
			if (flag4 && !list.Contains("Calendar5959a2259161d16d23a4f272"))
			{
				list.Add("Calendar5959a2259161d16d23a4f272");
			}
			if (base.LogicType == LogicType.Not)
			{
				listOrGroupRule = new ListOrGroupRule
				{
					not = new List<object>()
				};
				if (list.Count > 0)
				{
					ListRule item = new ListRule
					{
						or = list
					};
					listOrGroupRule.not.Add(item);
				}
				if (groupIds.Count > 0)
				{
					GroupRule item2 = new GroupRule
					{
						or = groupIds
					};
					listOrGroupRule.not.Add(item2);
				}
				if (list2.Count > 0)
				{
					BindAccountRule item3 = new BindAccountRule
					{
						or = list2
					};
					listOrGroupRule.not.Add(item3);
				}
				if (list3.Count > 0)
				{
					SubscribeCalendarRule item4 = new SubscribeCalendarRule
					{
						or = list3
					};
					listOrGroupRule.not.Add(item4);
				}
			}
			else
			{
				listOrGroupRule = new ListOrGroupRule
				{
					or = new List<object>()
				};
				if (list.Count > 0)
				{
					ListRule item5 = new ListRule
					{
						or = list
					};
					listOrGroupRule.or.Add(item5);
				}
				if (groupIds.Count > 0)
				{
					GroupRule item6 = new GroupRule
					{
						or = groupIds
					};
					listOrGroupRule.or.Add(item6);
				}
				if (list2.Count > 0)
				{
					BindAccountRule item7 = new BindAccountRule
					{
						or = list2
					};
					listOrGroupRule.or.Add(item7);
				}
				if (list3.Count > 0)
				{
					SubscribeCalendarRule item8 = new SubscribeCalendarRule
					{
						or = list3
					};
					listOrGroupRule.or.Add(item8);
				}
			}
		}
		return listOrGroupRule;
	}

	public bool RequiresVersion7()
	{
		ListOrGroupRule listOrGroupRule = ToRule() as ListOrGroupRule;
		if (!ContainsVersion7Rule(listOrGroupRule?.or))
		{
			return ContainsVersion7Rule(listOrGroupRule?.not);
		}
		return true;
	}

	private static bool ContainsVersion7Rule(IEnumerable<object> rules)
	{
		if (rules == null)
		{
			return false;
		}
		foreach (object rule in rules)
		{
			if (rule is ListRule listRule)
			{
				List<string> or = listRule.or;
				if (or != null && or.Contains("ProjectAll2e4c103c57ef480997943206"))
				{
					return true;
				}
			}
			if (rule is BindAccountRule bindAccountRule)
			{
				List<string> or2 = bindAccountRule.or;
				if (or2 != null && or2.Any())
				{
					goto IL_007c;
				}
			}
			if (!(rule is SubscribeCalendarRule subscribeCalendarRule))
			{
				continue;
			}
			List<string> or3 = subscribeCalendarRule.or;
			if (or3 == null || !or3.Any())
			{
				continue;
			}
			goto IL_007c;
			IL_007c:
			return true;
		}
		return false;
	}

	public override Rule<string> GetParseRule()
	{
		return new ListRule();
	}

	public override string ToCardText()
	{
		bool flag = ValueList.Contains("Calendar5959a2259161d16d23a4f272");
		bool flag2 = CalendarAccounts.Any((string id) => !string.IsNullOrEmpty(id)) || Calendars.Any((string id) => !string.IsNullOrEmpty(id) && id != "#allSubscribe");
		string text = ((base.LogicType == LogicType.Not) ? (Utils.GetString("Exclude") + " ") : string.Empty);
		if (ValueList.Any(CalendarUtils.IsAllListId) && !ValueList.Any((string id) => !CalendarUtils.IsAllListId(id) && id != "Calendar5959a2259161d16d23a4f272") && GroupIds.Count == 0 && !flag2)
		{
			string text2 = (flag ? Utils.GetString("All") : Utils.GetString("AllList"));
			return text + text2;
		}
		if (ValueList.Count == 0 && GroupIds.Count == 0)
		{
			return Utils.GetString("lists");
		}
		List<ProjectModel> projects = CacheManager.GetProjects();
		string divider = ((base.LogicType == LogicType.Not) ? ", " : (" " + Utils.GetString("or") + " "));
		if (flag2)
		{
			string text3 = ProjectExtra.FormatDisplayText(ProjectExtra.Serialize(new ProjectExtra
			{
				ProjectIds = ValueList.Where((string id) => id != "Calendar5959a2259161d16d23a4f272").ToList(),
				GroupIds = GroupIds.ToList(),
				BindAccounts = CalendarAccounts.ToList(),
				SubscribeCalendars = Calendars.ToList()
			}));
			if (!string.IsNullOrEmpty(text))
			{
				return text + "( " + text3 + " )";
			}
			return text3;
		}
		if (flag && ValueList.All((string id) => id == "Calendar5959a2259161d16d23a4f272") && GroupIds.Count == 0)
		{
			return text + Utils.GetString("AllCalendars", "All Calendars");
		}
		List<ProjectModel> list = projects.Where((ProjectModel p) => ValueList.Contains(p.id) || GroupIds.Contains(p.groupId)).ToList();
		if (ValueList.Contains("Calendar5959a2259161d16d23a4f272"))
		{
			list.Add(new ProjectModel
			{
				id = "Calendar5959a2259161d16d23a4f272",
				name = Utils.GetString("Calendar")
			});
		}
		switch (list.Count)
		{
		case 0:
			return Utils.GetString("Expired");
		case 1:
			return text + list[0].name;
		default:
		{
			string text4 = string.Empty;
			if (list.Count > 0)
			{
				text4 = ((list.Count > 3) ? (GetProjectFullText(list.Take(3).ToList(), text4, divider) + " " + Utils.GetString("or") + " " + string.Format(Utils.GetString("OtherProjects"), list.Count - 3)) : GetProjectFullText(list, text4, divider));
			}
			if (string.IsNullOrEmpty(text))
			{
				return text4;
			}
			return text + "( " + text4 + " )";
		}
		}
	}

	private static string GetProjectFullText(IReadOnlyList<ProjectModel> selectedProjects, string displayText, string divider = "")
	{
		for (int i = 0; i < selectedProjects.Count; i++)
		{
			displayText = ((i >= selectedProjects.Count - 1) ? (displayText + selectedProjects[i].name) : (displayText + selectedProjects[i].name + divider));
		}
		return displayText;
	}
}
