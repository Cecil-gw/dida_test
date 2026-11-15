using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class BindAccountRule : StringRule
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public new object conditionType;

	public override string conditionName => "account";
}
