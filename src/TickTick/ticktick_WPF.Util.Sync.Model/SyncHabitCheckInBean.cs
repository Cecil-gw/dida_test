using System.Collections.Generic;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncHabitCheckInBean
{
	[JsonProperty(PropertyName = "update")]
	public List<HabitCheckInModel> Update { get; set; } = new List<HabitCheckInModel>();

	[JsonProperty(PropertyName = "add")]
	public List<HabitCheckInModel> Add { get; set; } = new List<HabitCheckInModel>();

	[JsonProperty(PropertyName = "delete")]
	public List<HabitCheckInModel> Delete { get; set; } = new List<HabitCheckInModel>();
}
