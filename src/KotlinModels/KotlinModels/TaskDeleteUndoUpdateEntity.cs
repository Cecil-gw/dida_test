using System.Collections.Generic;

namespace KotlinModels;

public class TaskDeleteUndoUpdateEntity
{
	public long id;

	public string sid;

	public int taskStatus;

	public TTCalendar startDate;

	public TTCalendar dueDate;

	public TTCalendar snoozeRemindTime;

	public TTCalendar repeatFirstDate;

	public List<string> exDate = new List<string>();

	public string repeatFlag;
}
