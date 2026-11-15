using System;

namespace ticktick_WPF.Resource;

public class Constants
{
	public enum AttachmentKind
	{
		IMAGE,
		AUDIO,
		VIDEO,
		PDF,
		DOC,
		TEXT,
		XLS,
		PPT,
		CSV,
		ZIP,
		OTHER
	}

	public enum BindAccountType
	{
		CalDAV,
		Exchange
	}

	public enum DetailMode
	{
		Page,
		Popup,
		Editor,
		Sticky
	}

	public enum LimitKind
	{
		ProjectNumber,
		ProjectTaskNumber,
		SubtaskNumber,
		ShareUserNumber,
		DailyUploadNumber,
		TaskAttachmentNumber,
		ReminderNumber,
		AttachmentSize,
		KanbanNumber,
		HabitNumber,
		TimerNumber,
		VisitorNumber,
		CountdownNumber,
		MatrixProfileNumber,
		DailyReminderNumber,
		TeamNumber,
		TeamMemberNumber,
		HabitSectionNumber,
		NotionWorkspaceNumber,
		NotionDatabaseNumber,
		GoogleCalendarNumber,
		GoogleConnectNumber,
		CalendarBindNumber,
		UrlCalendarNumber,
		HolidayNumber
	}

	public enum ModelEditType
	{
		New,
		Edit
	}

	public enum ProjectPermission
	{
		write = 1,
		comment,
		read,
		person
	}

	public enum OrderDirection
	{
		Asc,
		Desc
	}

	public enum SortType
	{
		none = -1,
		sortOrder,
		project,
		dueDate,
		title,
		priority,
		assignee,
		tag,
		createdTime,
		modifiedTime
	}

	public enum ProjectKind
	{
		TASK,
		NOTE
	}

	public enum SyncStatus
	{
		SYNC_NEW,
		SYNC_UPDATE,
		SYNC_DONE,
		SYNC_INIT,
		SYNC_ERROR_UP_LIMIT,
		SYNC_INIT_UP_LIMIT
	}

	public class Priority
	{
		public const int High = 5;

		public const int Normal = 3;

		public const int Low = 1;

		public const int No = 0;

		public const int None = -1;
	}

	public class DurationValue
	{
		public const int QuarterHour = 15;

		public const int HalfHour = 30;

		public const int Hour = 60;

		public const int HourAndHalf = 90;

		public const int TwoHours = 120;

		public const int TwoHoursAndHalf = 150;

		public const int ThreeHours = 180;

		public const int OneDay = 1440;

		public const int TwoDays = 2880;

		public const int ThreeDays = 4320;

		public static int ToIndex(int value)
		{
			if (value == 15)
			{
				return 0;
			}
			if (value < 1440)
			{
				return value / 30;
			}
			return value / 1440 + 6;
		}

		public static int ToValue(int index)
		{
			if (index == 0)
			{
				return 15;
			}
			if (index <= 6)
			{
				return 30 * index;
			}
			return (index - 6) * 1440;
		}

		public static bool IsAllDayValue(int value)
		{
			return value % 1440 == 0;
		}
	}

	public class DateMode
	{
		public const int Date = 0;

		public const int Duration = 1;
	}

	public class DateValue
	{
		public const string None = "none";

		public const string Today = "today";

		public const string Tomorrow = "tomorrow";

		public const string DayAfterTomorrow = "day_after_tomorrow";

		public const string NextWeek = "NextWeek";

		public static int ToSelectIndex(string value)
		{
			return value switch
			{
				"none" => 0, 
				"today" => 1, 
				"tomorrow" => 2, 
				"day_after_tomorrow" => 3, 
				"NextWeek" => 4, 
				_ => 0, 
			};
		}

		public static int ToIndex(string value)
		{
			return value switch
			{
				"none" => 0, 
				"today" => 1, 
				"tomorrow" => 2, 
				"day_after_tomorrow" => 3, 
				"NextWeek" => 7, 
				_ => 0, 
			};
		}

		public static string ToValue(int index)
		{
			try
			{
				return index switch
				{
					0 => "none", 
					1 => "today", 
					2 => "tomorrow", 
					3 => "day_after_tomorrow", 
					4 => "NextWeek", 
					7 => "NextWeek", 
					_ => "none", 
				};
			}
			catch (Exception)
			{
				return "none";
			}
		}
	}

	public class TaskAddTo
	{
		public const int Top = 0;

		public const int Bottom = 1;
	}

	public static class SyncType
	{
		public const int Auto = 0;

		public const int Manual = 1;
	}

	public const string Visible = "Visible";

	public const string Collapsed = "Collapsed";

	public const string True = "True";

	public const string False = "False";

	public const string Text = "TEXT";

	public const string Checklist = "CHECKLIST";

	public const string Note = "NOTE";

	public static string DefaultFont = "Microsoft YaHei UI, Microsoft JhengHei, Segoe UI";

	public static string DefaultMonoFont = "Consolas";

	public const int SyncTypeTaskContent = 0;

	public const int SyncTypeTaskOrder = 1;

	public const int SyncTypeTaskMove = 2;

	public const int SyncTypeTaskAssign = 3;

	public const int SyncTypeTaskCreate = 4;

	public const int SyncTypeTaskTrash = 5;

	public const int SyncTypeTaskDeleteForever = 6;

	public const int SyncTypeTaskRestore = 7;

	public const int SyncTypeClearTrash = 8;

	public const int SyncTypeEventModified = 9;

	public const int SyncTypeEventCreate = 10;

	public const int SyncTypeEventTrash = 11;

	public const int SyncTypeEventMove = 12;

	public const int SyncTypeTemplateAdd = 13;

	public const int SyncTypeTemplateUpdate = 14;

	public const int SyncTypeTemplateDelete = 15;

	public const int SyncTypeTaskSetParent = 16;

	public const int SyncTypeColumnMoveProject = 32;

	public const long Step = 268435456L;

	public const string LogicAnd = "and";

	public const string LogicOr = "or";

	public const string FilterTypeList = "list";

	public const string FilterTypeGroup = "group";

	public const string FilterTypeBindAccount = "account";

	public const string FilterTypeSubscribeCalendar = "calendar";

	public const string FilterTypeBindAccountLegacy = "bind_account";

	public const string FilterTypeSubscribeCalendarLegacy = "subscribe_calendar";

	public const string FilterTypeListOrGroup = "listOrGroup";

	public const string FilterTypeTag = "tag";

	public const string FilterTypeDueDate = "dueDate";

	public const string FilterTypeAssignee = "assignee";

	public const string FilterTypePriority = "priority";

	public const string FilterTypeTaskType = "taskType";

	public const string FilterTypeKeywords = "keywords";

	public const string FilterTypeStatus = "status";

	public const string FilterTypeDate = "date";

	public const string FilterTypeCompletedTime = "completedTime";

	public const string AllTag = "*tag";

	public const string NoTag = "!tag";

	public const string WithTag = "*withtags";

	public const string NoDate = "nodue";

	public const string Overdue = "overdue";

	public const string Today = "today";

	public const string Tomorrow = "tomorrow";

	public const string ThisWeek = "thisweek";

	public const string Recurring = "recurring";

	public const string NextWeek = "nextweek";

	public const string ThisMonth = "thismonth";

	public const string FilterOffset = "offset({0})";

	public const string NextNDays = "{0}days";

	public const string NDaysLater = "{0}dayslater";

	public const string NDaysAfter = "{0}daysfromtoday";

	public const string NDaysAgo = "-{0}daysfromtoday";

	public const string DaysFromToday = "daysfromtoday";

	public const string Span = "span";

	public const string DaysSpan = "span({0}~{1})";

	public const string All = "all";

	public const string AssignToMe = "me";

	public const string AssignToOther = "other";

	public const string NoAssignee = "noassignee";

	public const string Assigned = "anyone";

	public const int DescMaxLength = 2048;

	public const string TagIdentifier = "#";

	public const string TagIdentifierExtra = "＃";

	public const string ProjectIdentifier = "^";

	public const string ProjectIdentifierExtra = "~";

	public const string PriorityIdentifier = "!";

	public const string PriorityIdentifierExtra = "！";

	public const string DateIdentifier = "*";

	public const string AssignIdentifier = "@";

	public const string TaskLinkIdentifier = "[[";

	public const string SplashIdentifier = "/";

	public const string SplashIdentifierExtra = "、";

	public const string CalendarSectionId = "8ac3038d93c54b80a67321b6a03df066";

	public const string PersonalTeamId = "c1a7e08345e444dea187e21a692f0d7a";

	public const string ClearTrashEntityId = "d4ae7f9fedd48aab729c2f9c1bccf46";

	public const string FilterCalendarId = "Calendar5959a2259161d16d23a4f272";

	public const string FilterHabitId = "Habit2e4c103c57ef480997943206";

	public const string FilterAllProjects = "ProjectAll2e4c103c57ef480997943206";

	public const bool CslSwitch = false;

	public const int DefaultMinFocusDuration = 300;

	public const int ShortMinFocusDuration = 60;

	public const int TitleMaxLength = 64;

	public const int TaskTitleMaxLength = 2048;

	public const int UploadTextMaxLength = 204800;

	public const int TaskContentMaxLength = 160000;

	public const int CheckItemContentMaxLength = 516;

	public const int TaskDeskContentMaxLength = 2048;

	public const int FilterCurrentVersion = 7;

	public const int MaxTimingHours = 12;

	public static int MinFocusDuration
	{
		get
		{
			if (!LocalSettings.Settings.PomoLocalSetting.HasSyncedShortFocus)
			{
				return 300;
			}
			return 60;
		}
	}
}
