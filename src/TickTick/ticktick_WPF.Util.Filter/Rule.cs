using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class Rule<T>
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<T> and;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public object conditionType = 1;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<T> not;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public List<T> or;

	public virtual string conditionName { get; set; }
}
