using TickTickModels;

namespace ticktick_WPF.Models;

public class LimitsModel : BaseModel
{
	public string type { get; set; }

	public int projectNumber { get; set; }

	public int projectTaskNumber { get; set; }

	public int subtaskNumber { get; set; }

	public int shareUserNumber { get; set; }

	public int dailyUploadNumber { get; set; }

	public int taskAttachmentNumber { get; set; }

	public int reminderNumber { get; set; }

	public int dailyReminderNumber { get; set; }

	public int attachmentSize { get; set; }

	public int kanbanNumber { get; set; }

	public int habitNumber { get; set; }

	public int timerNumber { get; set; }

	public int visitorNumber { get; set; }

	public int countdownNumber { get; set; }

	public int matrixProfileNumber { get; set; }

	public int teamNumber { get; set; }

	public int teamMemberNumber { get; set; }

	public int habitSectionNumber { get; set; }

	public int notionWorkspaceNumber { get; set; }

	public int notionDatabaseNumber { get; set; }

	public int googleCalendarNumber { get; set; }

	public int googleConnectNumber { get; set; }

	public int calendarBindNumber { get; set; }

	public int urlCalendarNumber { get; set; }

	public int holidayNumber { get; set; }
}
