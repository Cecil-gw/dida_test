using System.Collections.Generic;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncHabitRecordBean
{
	[JsonProperty(PropertyName = "add")]
	public List<HabitRecordModel> Add { get; set; } = new List<HabitRecordModel>();

	[JsonProperty(PropertyName = "update")]
	public List<HabitRecordModel> Update { get; set; } = new List<HabitRecordModel>();

	[JsonProperty(PropertyName = "delete")]
	public List<HabitRecordModel> Delete { get; set; } = new List<HabitRecordModel>();
}
