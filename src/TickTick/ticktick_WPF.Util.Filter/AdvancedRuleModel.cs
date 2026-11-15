using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class AdvancedRuleModel : RuleModel
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<object> and;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<object> or;

	public new int type => 1;
}
