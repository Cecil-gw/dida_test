using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class CourseScheduleModel : BaseModel
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("copyOriginId")]
	public string CopyOriginId { get; set; }

	[Ignore]
	[JsonProperty("courses")]
	public List<CourseModel> Courses { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	[JsonProperty("createdTime")]
	public DateTime CreatedTime { get; set; }

	[JsonProperty("etag")]
	public string Etag { get; set; }

	[JsonProperty("lessonTimes")]
	[Ignore]
	public Dictionary<int, string[]> LessonTimes { get; set; }

	[JsonIgnore]
	public string LessonTimesStr { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("schoolId")]
	public string SchoolId { get; set; }

	[JsonConverter(typeof(UtcDateTimeConverter))]
	[JsonProperty("startDate")]
	public DateTime StartDate { get; set; }

	[JsonProperty("sortOrder")]
	public long SortOrder { get; set; }

	[JsonProperty("weekCount")]
	public int WeekCount { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonIgnore]
	public int SyncStatus { get; set; }

	[JsonProperty("reminders")]
	[Ignore]
	public string[] Reminders { get; set; }

	[JsonIgnore]
	public string RemindersStr { get; set; }

	public bool AllTimeSet()
	{
		try
		{
			Dictionary<int, string[]> dictionary = JsonConvert.DeserializeObject<Dictionary<int, string[]>>(LessonTimesStr);
			if (dictionary != null && dictionary.Count > 0)
			{
				int num = dictionary.Keys.Max();
				if (dictionary.Count < num)
				{
					return false;
				}
			}
			return dictionary?.All((KeyValuePair<int, string[]> kv) => kv.Value != null && kv.Value.Length == 2) ?? false;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
