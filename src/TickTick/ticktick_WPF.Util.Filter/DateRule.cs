using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class DateRule : StringRule
{
	public new object conditionType = 1;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public new List<object> or;

	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public new List<object> not;

	public override string conditionName => "date";
}
