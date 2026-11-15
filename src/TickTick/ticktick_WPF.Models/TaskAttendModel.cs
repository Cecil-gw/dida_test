using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class TaskAttendModel
{
	public string id;

	public ProfileModel organizer;

	public List<AttendeeModel> attendees;
}
