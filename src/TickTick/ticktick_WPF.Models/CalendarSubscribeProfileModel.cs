using System;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;
using ticktick_WPF.Models.Interface;

namespace ticktick_WPF.Models;

[Serializable]
public class CalendarSubscribeProfileModel : ErrorModel, OrderInterface
{
	[JsonProperty("id")]
	public string Id { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; } = string.Empty;

	[JsonProperty("url")]
	public string Url { get; set; }

	[JsonProperty("show")]
	public string Show { get; set; }

	[Ignore]
	[JsonIgnore]
	public CalendarSubsShowType ShowType
	{
		get
		{
			if (Enum.TryParse<CalendarSubsShowType>(Show, ignoreCase: true, out var result))
			{
				return result;
			}
			return CalendarSubsShowType.show;
		}
		set
		{
			Show = value.ToString();
		}
	}

	[JsonProperty("createdTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? CreatedTime { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonProperty("color")]
	public string Color { get; set; }

	[JsonProperty("kind")]
	public int? Kind { get; set; }

	[JsonProperty("reminderEnable")]
	public bool? ReminderEnable { get; set; }

	public bool Expired { get; set; }

	[JsonProperty("order")]
	public long? Order { get; set; }
}
