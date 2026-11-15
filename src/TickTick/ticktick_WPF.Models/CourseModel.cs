using System.Collections.Generic;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class CourseModel : BaseModel
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("color")]
	public string Color { get; set; }

	[Ignore]
	[JsonProperty("items")]
	public List<CourseDetailModel> Items { get; set; }

	[JsonIgnore]
	public string ItemsStr { get; set; }

	[JsonIgnore]
	public string ScheduleId { get; set; }
}
