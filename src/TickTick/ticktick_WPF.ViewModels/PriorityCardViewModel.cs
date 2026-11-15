using System.Collections.Generic;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Filter;

namespace ticktick_WPF.ViewModels;

public class PriorityCardViewModel : FilterListDisplayModel<int>
{
	private static readonly Dictionary<int, string> PriorityMap = new Dictionary<int, string>
	{
		{
			0,
			Utils.GetString("PriorityNull")
		},
		{
			1,
			Utils.GetString("PriorityLow")
		},
		{
			3,
			Utils.GetString("PriorityMedium")
		},
		{
			5,
			Utils.GetString("PriorityHigh")
		}
	};

	public List<int> Values
	{
		get
		{
			return ValueList;
		}
		set
		{
			ValueList = value;
			base.Content = ToDisplayList(ValueList);
			OnPropertyChanged("Values");
		}
	}

	public PriorityCardViewModel()
	{
		base.ConditionName = "priority";
	}

	private static string ToDisplayList(List<int> priorities)
	{
		string text = string.Empty;
		if (priorities != null && priorities.Count > 0)
		{
			for (int i = 0; i < priorities.Count; i++)
			{
				text = ((i >= priorities.Count - 1) ? (text + PriorityMap[priorities[i]]) : (text + PriorityMap[priorities[i]] + " " + Utils.GetString("or") + " "));
			}
		}
		return text;
	}

	public override ConditionEditDialog GenerateEditDialog()
	{
		return new PriorityEditDialog();
	}

	public override Rule<int> GetParseRule()
	{
		return new PriorityRule();
	}

	public override string ToCardText()
	{
		string text = ToCardText(ValueList);
		if (base.LogicType != LogicType.Not || ValueList.Count <= 0)
		{
			return text;
		}
		return Utils.GetString("Exclude") + " " + text;
	}

	public static string ToCardText(List<int> priorities)
	{
		return priorities.Count switch
		{
			0 => Utils.GetString("priority"), 
			1 => PriorityMap[priorities[0]], 
			_ => ToDisplayList(priorities), 
		};
	}
}
