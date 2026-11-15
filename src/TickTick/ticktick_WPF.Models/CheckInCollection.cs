using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class CheckInCollection
{
	[JsonProperty(PropertyName = "checkins")]
	public Dictionary<string, List<HabitCheckInModel>> CheckIns;
}
