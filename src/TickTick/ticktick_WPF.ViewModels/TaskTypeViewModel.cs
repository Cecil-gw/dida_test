using System.Collections.Generic;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;
using ticktick_WPF.Views.Filter;

namespace ticktick_WPF.ViewModels;

public class TaskTypeViewModel : FilterListDisplayModel<string>
{
	private static readonly Dictionary<string, string> TaskTypeMap = new Dictionary<string, string>
	{
		{
			"task",
			Utils.GetString("Task")
		},
		{
			"note",
			Utils.GetString("Notes")
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

	public bool IsNote
	{
		get
		{
			List<string> values = Values;
			if (values != null && values.Count == 1)
			{
				return Values[0] == "note";
			}
			return false;
		}
	}

	public TaskTypeViewModel()
	{
		base.ConditionName = "taskType";
	}

	public static string ToDisplayList(List<string> types)
	{
		string text = string.Empty;
		if (types != null && types.Count > 0)
		{
			for (int i = 0; i < types.Count; i++)
			{
				text = ((i >= types.Count - 1) ? (text + TaskTypeMap[types[i]]) : (text + TaskTypeMap[types[i]] + " " + Utils.GetString("or") + " "));
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
		string text = ((base.LogicType == LogicType.Not && ValueList.Count > 0) ? (Utils.GetString("Exclude") + " ") : string.Empty);
		return ValueList.Count switch
		{
			0 => Utils.GetString("TaskType"), 
			1 => text + TaskTypeMap[ValueList[0]], 
			_ => text + ToDisplayList(ValueList), 
		};
	}

	public override Rule<string> GetParseRule()
	{
		return new TaskTypeRule();
	}
}
