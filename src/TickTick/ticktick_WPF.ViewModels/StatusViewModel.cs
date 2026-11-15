using System.Collections.Generic;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Filter;

namespace ticktick_WPF.ViewModels;

public class StatusViewModel : FilterListDisplayModel<string>
{
	private static readonly Dictionary<string, string> StatusTypeMap = new Dictionary<string, string>
	{
		{
			"uncompleted",
			Utils.GetString("Uncompleted")
		},
		{
			"inProgress",
			Utils.GetString("SummaryInProgress")
		},
		{
			"completed",
			Utils.GetString("Completed")
		},
		{
			"wontDo",
			Utils.GetString("Abandoned")
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

	public StatusViewModel()
	{
		base.ConditionName = "status";
	}

	public static string ToDisplayList(List<string> types)
	{
		string text = string.Empty;
		if (types != null && types.Count > 0)
		{
			for (int i = 0; i < types.Count; i++)
			{
				text = ((i >= types.Count - 1) ? (text + StatusTypeMap[types[i]]) : (text + StatusTypeMap[types[i]] + " " + Utils.GetString("or") + " "));
			}
		}
		return text;
	}

	public override ConditionEditDialog GenerateEditDialog()
	{
		return new PriorityEditDialog();
	}

	public override string ToCardText()
	{
		return ToDisplayList(ValueList);
	}

	public override Rule<string> GetParseRule()
	{
		return new StatusRule();
	}
}
