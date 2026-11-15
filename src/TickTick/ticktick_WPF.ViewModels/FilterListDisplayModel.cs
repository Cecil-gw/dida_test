using System.Collections.Generic;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Filter;

namespace ticktick_WPF.ViewModels;

public abstract class FilterListDisplayModel<T> : CardViewModel
{
	protected List<T> ValueList = new List<T>();

	protected FilterListDisplayModel()
	{
		base.Type = CardType.Filter;
	}

	public virtual Rule<T> ToRule()
	{
		Rule<T> parseRule = GetParseRule();
		SetRuleValue(parseRule);
		return parseRule;
	}

	public abstract Rule<T> GetParseRule();

	protected void SetRuleValue(Rule<T> rule)
	{
		if (base.LogicType == LogicType.And)
		{
			rule.and = ValueList;
		}
		else if (base.LogicType == LogicType.Or)
		{
			rule.or = ValueList;
		}
		else
		{
			rule.not = ValueList;
		}
	}

	protected static string GetLogicString(LogicType logic)
	{
		return logic switch
		{
			LogicType.And => Utils.GetString("and"), 
			LogicType.Or => Utils.GetString("or"), 
			LogicType.Not => Utils.GetString("Exclude"), 
			_ => string.Empty, 
		};
	}
}
