using System.Collections.Generic;
using ticktick_WPF.Util.Filter;

namespace ticktick_WPF.ViewModels;

public class KeywordsViewModel : FilterListDisplayModel<string>
{
	public bool IsNewAdd;

	public string Keyword
	{
		get
		{
			List<string> valueList = ValueList;
			if (valueList == null || valueList.Count <= 0)
			{
				return string.Empty;
			}
			return ValueList[0];
		}
		set
		{
			ValueList = new List<string> { value };
		}
	}

	public KeywordsViewModel()
	{
		base.ConditionName = "keywords";
	}

	public override Rule<string> GetParseRule()
	{
		return new KeywordsRule();
	}
}
