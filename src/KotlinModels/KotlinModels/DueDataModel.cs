using System.Collections.Generic;

namespace KotlinModels;

public class DueDataModel
{
	public string repeatFlag;

	public string repeatFrom;

	public string timeZone;

	public List<Reminder> reminders = new List<Reminder>();

	public int isAllDay;

	public int isFloating;

	public int isClearDate;

	public TTCalendar startDate;

	public TTCalendar dueDate;

	public TTCalendar completedDate;

	public TTCalendar repeatOriginStartDate;

	public List<TTCalendar> exDate = new List<TTCalendar>();
}
