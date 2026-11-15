using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class ListRuleModel
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<object> and;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public string conditionName;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public int conditionType;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<object> not;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<object> or;
}
