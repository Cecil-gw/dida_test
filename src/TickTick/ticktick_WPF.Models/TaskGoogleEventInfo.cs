using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

public class TaskGoogleEventInfo : BaseModel
{
	[Indexed]
	public string id { get; set; }

	[Indexed]
	public string userId { get; set; }

	[Indexed]
	public string projectId { get; set; }

	[Indexed]
	public string taskId { get; set; }

	[Indexed]
	public string connectId { get; set; }

	public string calendarId { get; set; }

	public string eventId { get; set; }

	public string location { get; set; }

	[Ignore]
	[JsonProperty("attendees")]
	public List<TaskGoogleEventAttendee> attendees { get; set; }

	[JsonIgnore]
	[Column("attendees")]
	public string attendeesString { get; set; }

	[Ignore]
	[JsonProperty("meeting")]
	public TaskGoogleMeetingInfo meeting { get; set; }

	[JsonIgnore]
	[Column("meeting")]
	public string meetingString { get; set; }

	public string htmlLink { get; set; }

	public string etag { get; set; }

	public bool deleted { get; set; }

	public long etimestamp { get; set; }

	public DateTime? createdTime { get; set; }

	public DateTime? modifiedTime { get; set; }

	public void PrepareForStorage()
	{
		attendeesString = ((attendees == null) ? null : JsonConvert.SerializeObject(attendees));
		meetingString = ((meeting == null) ? null : JsonConvert.SerializeObject(meeting));
	}

	public void RestoreNestedData()
	{
		try
		{
			attendees = (string.IsNullOrEmpty(attendeesString) ? null : JsonConvert.DeserializeObject<List<TaskGoogleEventAttendee>>(attendeesString));
		}
		catch (JsonException)
		{
			attendees = null;
		}
		try
		{
			meeting = (string.IsNullOrEmpty(meetingString) ? null : JsonConvert.DeserializeObject<TaskGoogleMeetingInfo>(meetingString));
		}
		catch (JsonException)
		{
			meeting = null;
		}
	}

	public bool HasDisplayContent()
	{
		if (string.IsNullOrWhiteSpace(location))
		{
			List<TaskGoogleEventAttendee> list = attendees;
			if (list == null || !list.Any((TaskGoogleEventAttendee attendee) => attendee != null && (!string.IsNullOrWhiteSpace(attendee.email) || !string.IsNullOrWhiteSpace(attendee.displayName))))
			{
				if (meeting != null)
				{
					if (string.IsNullOrWhiteSpace(meeting.name))
					{
						return !string.IsNullOrWhiteSpace(meeting.url);
					}
					return true;
				}
				return false;
			}
		}
		return true;
	}
}
