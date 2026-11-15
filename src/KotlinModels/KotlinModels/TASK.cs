using System.Collections.Generic;

namespace KotlinModels;

public class TASK
{
	public long uniqueId;

	public long completedUserId;

	public long projectUid;

	public long sortOrder;

	public string id;

	public string projectId;

	public string parentId;

	public string attendId;

	public string repeatFlag;

	public string repeatTaskId;

	public string repeatFrom;

	public string kind;

	public string timeZone;

	public string userId;

	public List<string> exDate;

	public List<string> childIds;

	public List<Reminder> reminders;

	public List<CheckItem> items;

	public int taskStatus;

	public int isAllDay;

	public int isFloating;

	public int progress;

	public TTCalendar startDate;

	public TTCalendar dueDate;

	public TTCalendar completedDate;

	public TTCalendar repeatFirstDate;

	public TTCalendar recurringStartDate;

	public TTCalendar recurringDueDate;

	public TTCalendar tempStartDate;

	public TTCalendar tempDueDate;

	public TTCalendar modifiedTime;

	public TTCalendar snoozeRemindTime;
}
