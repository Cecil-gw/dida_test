using Newtonsoft.Json;

namespace ticktick_WPF.Util.Filter;

public class SubscribeCalendarRule : StringRule
{
	[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
	public new object conditionType;

	public override string conditionName => "calendar";
}
