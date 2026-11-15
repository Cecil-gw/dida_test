using System;
using System.Collections.Generic;
using System.Linq;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Filter;

namespace ticktick_WPF.ViewModels;

public class AssigneeCardViewModel : FilterListDisplayModel<string>
{
	private static readonly Dictionary<string, string> AssigneeMap = new Dictionary<string, string>
	{
		{
			"me",
			Utils.GetString("AssignToMe")
		},
		{
			"other",
			Utils.GetString("AssignedToOthers2")
		},
		{
			"noassignee",
			Utils.GetString("NotAssigned")
		},
		{
			"anyone",
			Utils.GetString("Assigned")
		}
	};

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

	public AssigneeCardViewModel()
	{
		base.ConditionName = "assignee";
	}

	private static string GetValueTitle(string val)
	{
		if (!AssigneeMap.ContainsKey(val))
		{
			return AvatarHelper.GetUserModelById(val)?.displayName;
		}
		return AssigneeMap[val];
	}

	public static string GetAssigneeTitle(string assignee)
	{
		return GetValueTitle(assignee);
	}

	public static string GetAssigneeValueText(List<string> assignees)
	{
		switch (assignees.Count)
		{
		case 0:
			return Utils.GetString("assignee");
		case 1:
			return GetValueTitle(assignees[0]);
		default:
		{
			string text = string.Empty;
			int num = Math.Min(3, assignees.Count);
			for (int i = 0; i < num; i++)
			{
				string valueTitle = GetValueTitle(assignees[i]);
				text = ((i >= num - 1) ? (text + valueTitle) : (text + valueTitle + " " + Utils.GetString("or") + " "));
			}
			if (assignees.Count > 3)
			{
				text = string.Format(Utils.GetString("AssignDisplayMessage"), text, assignees.Count - 3);
			}
			return text;
		}
		}
	}

	public override string ToCardText()
	{
		List<string> valueList = ValueList;
		string assigneeValueText = GetAssigneeValueText(valueList);
		if (base.LogicType != LogicType.Not || valueList.Count <= 0)
		{
			return assigneeValueText;
		}
		return Utils.GetString("Exclude") + " " + assigneeValueText;
	}

	public override ConditionEditDialog GenerateEditDialog()
	{
		return new AssigneeEditDialog(Version);
	}

	public override Rule<string> GetParseRule()
	{
		return new AssigneeRule();
	}

	public static string ToNormalDisplayText(List<string> assignees, int displayNum = 3)
	{
		return ToAssigneeText(assignees, ", ", displayNum);
	}

	private static string ToAssigneeText(List<string> assignees, string divider, int displayNum = 3)
	{
		if (assignees.Count == 0)
		{
			return Utils.GetString("All");
		}
		if (assignees.Count <= displayNum)
		{
			return DisplayAssigneeText(assignees, divider);
		}
		return string.Format(Utils.GetString("AssignDisplayMessage"), DisplayAssigneeText(assignees.Take(displayNum).ToList(), divider), assignees.Count - displayNum);
	}

	private static string DisplayAssigneeText(IReadOnlyList<string> assignees, string divider)
	{
		string text = string.Empty;
		if (assignees != null && assignees.Count > 0)
		{
			for (int i = 0; i < assignees.Count; i++)
			{
				text = ((i >= assignees.Count - 1) ? (text + GetValueTitle(assignees[i])) : (text + GetValueTitle(assignees[i]) + divider));
			}
		}
		return text;
	}
}
