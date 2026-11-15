using System.Collections.Generic;
using System.Linq;

namespace KotlinModels;

public class TaskDeleteRecurrenceUndoEntity
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

	public TaskDeleteRecurrenceUndoEntity(TaskDeleteUndoUpdateEntity undo)
	{
		id = undo.id;
		sid = undo.sid;
		taskStatus = undo.taskStatus;
		startDate = undo.startDate;
		dueDate = undo.dueDate;
		repeatFirstDate = undo.repeatFirstDate;
		snoozeRemindTime = undo.snoozeRemindTime;
		exDate = undo.exDate.ToList();
		repeatFlag = undo.repeatFlag;
	}
}
