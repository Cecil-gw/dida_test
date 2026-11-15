using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class HabitRecordCheckResult
{
	[JsonProperty(PropertyName = "habitRecords")]
	public Dictionary<string, List<HabitRecordModel>> HabitRecords;
}
