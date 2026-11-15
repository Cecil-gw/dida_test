using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class NormalRuleModel : RuleModel
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<object> and;

	public new int type => 0;
}
