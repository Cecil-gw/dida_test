using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class ListRule : StringRule
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public new object conditionType;

	public override string conditionName => "list";
}
