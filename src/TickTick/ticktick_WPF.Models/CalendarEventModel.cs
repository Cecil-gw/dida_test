using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

[Serializable]
public class CalendarEventModel : BaseModel
{
	[JsonProperty("id")]
	[Indexed]
	public string Id { get; set; }

	[JsonProperty("uid")]
	public string Uid { get; set; }

	[JsonProperty("title")]
	public string Title { get; set; }

	[JsonProperty("content")]
	public string Content { get; set; }

	[JsonProperty("location")]
	public string Location { get; set; }

	[JsonProperty("dueStart")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? DueStart { get; set; }

	[JsonProperty("dueEnd")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? DueEnd { get; set; }

	[JsonProperty("originalStartTime")]
	[JsonConverter(typeof(UtcDateTimeConverter))]
	public DateTime? OriginalStartTime { get; set; }

	[JsonProperty("isAllDay")]
	public bool IsAllDay { get; set; }

	[JsonProperty("repeatFlag")]
	public string RepeatFlag { get; set; }

	[JsonProperty("recurrence")]
	public string Recurrence { get; set; }

	[JsonProperty("etag")]
	public string Etag { get; set; }

	[JsonProperty("tickTaskId")]
	public string TickTaskId { get; set; }

	[Ignore]
	[JsonProperty("conference")]
	public ConferenceModel Conference { get; set; }

	[JsonIgnore]
	public string ConferenceInfo
	{
		get
		{
			return JsonConvert.SerializeObject(Conference);
		}
		set
		{
			if (!string.IsNullOrEmpty(value))
			{
				Conference = JsonConvert.DeserializeObject<ConferenceModel>(value);
			}
		}
	}

	[JsonIgnore]
	public string CalendarId { get; set; }

	[JsonIgnore]
	public int Type { get; set; }

	[JsonIgnore]
	public string UserId { get; set; }

	[JsonIgnore]
	public string OriginalEventId { get; set; }

	[Ignore]
	[JsonProperty("attendees")]
	public List<CalendarAttendeeModel> Attendees { get; set; }

	[JsonIgnore]
	public string AttendeeInfo { get; set; }

	[JsonIgnore]
	public int Deleted { get; set; }

	[JsonProperty("timezone")]
	public string TimeZone { get; set; }

	[Ignore]
	[JsonProperty("reminders")]
	public List<int> ReminderList { get; set; }

	[JsonIgnore]
	public string Reminders { get; set; }

	[Ignore]
	[JsonIgnore]
	public bool Editable { get; set; }

	[Ignore]
	[JsonIgnore]
	public CalendarAvailableType AvailableType { get; set; }

	[Ignore]
	[JsonProperty("eXDates")]
	public List<DateTime> ExDateList { get; set; }

	[JsonIgnore]
	public string ExDates { get; set; }

	[JsonIgnore]
	[Indexed]
	public string EventId { get; set; } = string.Empty;

	[Ignore]
	[JsonIgnore]
	public int Status { get; set; }

	public string ResponseStatus { get; set; }

	[Ignore]
	[JsonIgnore]
	public ResponseStatusType ResponseStatusType
	{
		get
		{
			if (!Enum.TryParse<ResponseStatusType>(ResponseStatus, ignoreCase: true, out var result))
			{
				return ResponseStatusType.None;
			}
			return result;
		}
	}

	public static CalendarEventModel Copy(CalendarEventModel model)
	{
		return new CalendarEventModel
		{
			Id = model.Id,
			Uid = model.Uid,
			Title = model.Title,
			Content = model.Content,
			Conference = model.Conference,
			Location = model.Location,
			DueStart = model.DueStart,
			DueEnd = model.DueEnd,
			IsAllDay = model.IsAllDay,
			RepeatFlag = model.RepeatFlag,
			Etag = model.Etag,
			CalendarId = model.CalendarId,
			UserId = model.UserId,
			Attendees = model.Attendees,
			AttendeeInfo = model.AttendeeInfo,
			Deleted = model.Deleted,
			TimeZone = model.TimeZone,
			ReminderList = model.ReminderList,
			Reminders = model.Reminders,
			ExDateList = model.ExDateList,
			ExDates = model.ExDates,
			EventId = model.EventId
		};
	}

	public bool IsSkipped(List<CalendarEventModel> skipEvents, DateTime? date = null)
	{
		if (OriginalStartTime.HasValue && string.IsNullOrEmpty(RepeatFlag))
		{
			return false;
		}
		date = date ?? DueStart;
		return skipEvents?.Any(delegate(CalendarEventModel cal)
		{
			if (!IsSameGeneratedEvent(cal) && cal.OriginalStartTime.HasValue && date.HasValue && cal.OriginalStartTime.Value.Date == date.Value.Date)
			{
				if (!string.IsNullOrEmpty(EventId))
				{
					string eventId = cal.EventId;
					if (eventId != null && eventId.StartsWith(EventId + "_"))
					{
						return true;
					}
				}
				if (!string.IsNullOrEmpty(Uid))
				{
					return cal.Uid == Uid;
				}
				return false;
			}
			return false;
		}) ?? false;
	}

	private bool IsSameGeneratedEvent(CalendarEventModel cal)
	{
		if (cal == null || string.IsNullOrEmpty(cal.Id))
		{
			return false;
		}
		if (!(cal.Id == Id))
		{
			return Id?.StartsWith(cal.Id + "_time_", StringComparison.Ordinal) ?? false;
		}
		return true;
	}
}
