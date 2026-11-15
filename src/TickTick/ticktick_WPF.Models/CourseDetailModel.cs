using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class CourseDetailModel
{
	[JsonProperty("startLesson")]
	public int StartLesson { get; set; }

	[JsonProperty("endLesson")]
	public int EndLesson { get; set; }

	[JsonProperty("weekday")]
	public int Weekday { get; set; }

	[JsonProperty("room")]
	public string Room { get; set; }

	[JsonProperty("teacher")]
	public string Teacher { get; set; }

	[JsonProperty("weeks")]
	public int[] Weeks { get; set; }
}
