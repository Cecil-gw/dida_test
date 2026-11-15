using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Views;

namespace ticktick_WPF.Cache;

public static class LimitCache
{
	private static Dictionary<Constants.LimitKind, long> _proLimitData;

	private static Dictionary<Constants.LimitKind, long> _freeLimitData;

	private static Dictionary<Constants.LimitKind, long> _teamLimitData;

	public static long GetFreeLimitByKey(Constants.LimitKind limit)
	{
		Dictionary<Constants.LimitKind, long> freeLimitData = _freeLimitData;
		if (freeLimitData == null)
		{
			InitData();
			freeLimitData = _freeLimitData;
		}
		if (freeLimitData != null && freeLimitData.TryGetValue(limit, out var value))
		{
			return value;
		}
		throw new Exception("no limit type existed.");
	}

	public static long GetProLimitByKey(Constants.LimitKind limit)
	{
		Dictionary<Constants.LimitKind, long> proLimitData = _proLimitData;
		if (proLimitData == null)
		{
			InitData();
			proLimitData = _proLimitData;
		}
		if (proLimitData != null && proLimitData.ContainsKey(limit))
		{
			return proLimitData[limit];
		}
		throw new Exception("no limit type existed.");
	}

	public static long GetLimitByKey(Constants.LimitKind limit)
	{
		Dictionary<Constants.LimitKind, long> dictionary = ((!LocalSettings.Settings.IsPro) ? _freeLimitData : (UserManager.IsTeamActive() ? _teamLimitData : _proLimitData));
		if (dictionary == null)
		{
			InitData();
			dictionary = ((!LocalSettings.Settings.IsPro) ? _freeLimitData : (UserManager.IsTeamActive() ? _teamLimitData : _proLimitData));
		}
		if (dictionary != null && dictionary.ContainsKey(limit))
		{
			return dictionary[limit];
		}
		throw new Exception("no limit type existed.");
	}

	public static async Task InitLimitCache()
	{
		InitData();
		ObservableCollection<LimitsModel> observableCollection = await Communicator.GetLimits();
		if (observableCollection != null && observableCollection.Any())
		{
			LimitsModel limitsModel = observableCollection.FirstOrDefault((LimitsModel item) => item.type == "FREE");
			if (limitsModel != null)
			{
				UpdateDictByDate(_freeLimitData, limitsModel);
			}
			LimitsModel limitsModel2 = observableCollection.FirstOrDefault((LimitsModel item) => item.type == "PRO");
			if (limitsModel2 != null)
			{
				UpdateDictByDate(_proLimitData, limitsModel2);
			}
			LimitsModel limitsModel3 = observableCollection.FirstOrDefault((LimitsModel item) => item.type == "TEAM");
			if (limitsModel3 != null)
			{
				UpdateDictByDate(_teamLimitData, limitsModel3);
			}
		}
	}

	private static void UpdateDictByDate(Dictionary<Constants.LimitKind, long> dict, LimitsModel model)
	{
		dict[Constants.LimitKind.SubtaskNumber] = model.subtaskNumber;
		dict[Constants.LimitKind.ProjectTaskNumber] = model.projectTaskNumber;
		dict[Constants.LimitKind.ShareUserNumber] = model.shareUserNumber;
		dict[Constants.LimitKind.ProjectNumber] = model.projectNumber;
		dict[Constants.LimitKind.ReminderNumber] = model.reminderNumber;
		dict[Constants.LimitKind.AttachmentSize] = model.attachmentSize;
		dict[Constants.LimitKind.TaskAttachmentNumber] = model.taskAttachmentNumber;
		dict[Constants.LimitKind.DailyUploadNumber] = model.dailyUploadNumber;
		dict[Constants.LimitKind.KanbanNumber] = ((model.kanbanNumber > 0) ? model.kanbanNumber : 19);
		dict[Constants.LimitKind.HabitNumber] = model.habitNumber;
		dict[Constants.LimitKind.TimerNumber] = model.timerNumber;
		dict[Constants.LimitKind.VisitorNumber] = model.visitorNumber;
		dict[Constants.LimitKind.CountdownNumber] = model.countdownNumber;
		if (model.matrixProfileNumber > 0)
		{
			dict[Constants.LimitKind.MatrixProfileNumber] = model.matrixProfileNumber;
		}
		dict[Constants.LimitKind.DailyReminderNumber] = model.dailyReminderNumber;
		dict[Constants.LimitKind.TeamNumber] = model.teamNumber;
		dict[Constants.LimitKind.TeamMemberNumber] = model.teamMemberNumber;
		dict[Constants.LimitKind.HabitSectionNumber] = model.habitSectionNumber;
		dict[Constants.LimitKind.NotionWorkspaceNumber] = model.notionWorkspaceNumber;
		dict[Constants.LimitKind.NotionDatabaseNumber] = model.notionDatabaseNumber;
		dict[Constants.LimitKind.GoogleCalendarNumber] = model.googleCalendarNumber;
		dict[Constants.LimitKind.GoogleConnectNumber] = model.googleConnectNumber;
		dict[Constants.LimitKind.CalendarBindNumber] = model.calendarBindNumber;
		dict[Constants.LimitKind.UrlCalendarNumber] = model.urlCalendarNumber;
		dict[Constants.LimitKind.HolidayNumber] = model.holidayNumber;
	}

	private static void InitData()
	{
		_freeLimitData = new Dictionary<Constants.LimitKind, long>
		{
			{
				Constants.LimitKind.SubtaskNumber,
				19L
			},
			{
				Constants.LimitKind.ProjectTaskNumber,
				99L
			},
			{
				Constants.LimitKind.ProjectNumber,
				9L
			},
			{
				Constants.LimitKind.ShareUserNumber,
				2L
			},
			{
				Constants.LimitKind.ReminderNumber,
				2L
			},
			{
				Constants.LimitKind.AttachmentSize,
				10485760L
			},
			{
				Constants.LimitKind.TaskAttachmentNumber,
				20L
			},
			{
				Constants.LimitKind.DailyUploadNumber,
				2L
			},
			{
				Constants.LimitKind.KanbanNumber,
				19L
			},
			{
				Constants.LimitKind.HabitNumber,
				5L
			},
			{
				Constants.LimitKind.TimerNumber,
				3L
			},
			{
				Constants.LimitKind.VisitorNumber,
				1L
			},
			{
				Constants.LimitKind.CountdownNumber,
				5L
			},
			{
				Constants.LimitKind.MatrixProfileNumber,
				1L
			},
			{
				Constants.LimitKind.DailyReminderNumber,
				2L
			},
			{
				Constants.LimitKind.TeamNumber,
				1L
			},
			{
				Constants.LimitKind.TeamMemberNumber,
				999L
			},
			{
				Constants.LimitKind.HabitSectionNumber,
				18L
			},
			{
				Constants.LimitKind.NotionWorkspaceNumber,
				0L
			},
			{
				Constants.LimitKind.NotionDatabaseNumber,
				0L
			},
			{
				Constants.LimitKind.GoogleCalendarNumber,
				0L
			},
			{
				Constants.LimitKind.GoogleConnectNumber,
				0L
			},
			{
				Constants.LimitKind.CalendarBindNumber,
				0L
			},
			{
				Constants.LimitKind.UrlCalendarNumber,
				0L
			},
			{
				Constants.LimitKind.HolidayNumber,
				0L
			}
		};
		_proLimitData = new Dictionary<Constants.LimitKind, long>
		{
			{
				Constants.LimitKind.SubtaskNumber,
				199L
			},
			{
				Constants.LimitKind.ProjectTaskNumber,
				999L
			},
			{
				Constants.LimitKind.ProjectNumber,
				299L
			},
			{
				Constants.LimitKind.ShareUserNumber,
				29L
			},
			{
				Constants.LimitKind.ReminderNumber,
				5L
			},
			{
				Constants.LimitKind.AttachmentSize,
				20971520L
			},
			{
				Constants.LimitKind.TaskAttachmentNumber,
				39L
			},
			{
				Constants.LimitKind.DailyUploadNumber,
				199L
			},
			{
				Constants.LimitKind.KanbanNumber,
				19L
			},
			{
				Constants.LimitKind.HabitNumber,
				299L
			},
			{
				Constants.LimitKind.TimerNumber,
				49L
			},
			{
				Constants.LimitKind.VisitorNumber,
				29L
			},
			{
				Constants.LimitKind.CountdownNumber,
				299L
			},
			{
				Constants.LimitKind.MatrixProfileNumber,
				9L
			},
			{
				Constants.LimitKind.DailyReminderNumber,
				65535L
			},
			{
				Constants.LimitKind.TeamNumber,
				1L
			},
			{
				Constants.LimitKind.TeamMemberNumber,
				999L
			},
			{
				Constants.LimitKind.HabitSectionNumber,
				18L
			},
			{
				Constants.LimitKind.NotionWorkspaceNumber,
				3L
			},
			{
				Constants.LimitKind.NotionDatabaseNumber,
				9L
			},
			{
				Constants.LimitKind.GoogleCalendarNumber,
				3L
			},
			{
				Constants.LimitKind.GoogleConnectNumber,
				9L
			},
			{
				Constants.LimitKind.CalendarBindNumber,
				3L
			},
			{
				Constants.LimitKind.UrlCalendarNumber,
				5L
			},
			{
				Constants.LimitKind.HolidayNumber,
				10L
			}
		};
		_teamLimitData = new Dictionary<Constants.LimitKind, long>
		{
			{
				Constants.LimitKind.SubtaskNumber,
				199L
			},
			{
				Constants.LimitKind.ProjectTaskNumber,
				2000L
			},
			{
				Constants.LimitKind.ProjectNumber,
				499L
			},
			{
				Constants.LimitKind.ShareUserNumber,
				29L
			},
			{
				Constants.LimitKind.ReminderNumber,
				5L
			},
			{
				Constants.LimitKind.AttachmentSize,
				52428800L
			},
			{
				Constants.LimitKind.TaskAttachmentNumber,
				99L
			},
			{
				Constants.LimitKind.DailyUploadNumber,
				99L
			},
			{
				Constants.LimitKind.KanbanNumber,
				19L
			},
			{
				Constants.LimitKind.HabitNumber,
				299L
			},
			{
				Constants.LimitKind.TimerNumber,
				49L
			},
			{
				Constants.LimitKind.VisitorNumber,
				29L
			},
			{
				Constants.LimitKind.CountdownNumber,
				299L
			},
			{
				Constants.LimitKind.MatrixProfileNumber,
				9L
			},
			{
				Constants.LimitKind.DailyReminderNumber,
				65535L
			},
			{
				Constants.LimitKind.TeamNumber,
				1L
			},
			{
				Constants.LimitKind.TeamMemberNumber,
				999L
			},
			{
				Constants.LimitKind.HabitSectionNumber,
				18L
			},
			{
				Constants.LimitKind.NotionWorkspaceNumber,
				3L
			},
			{
				Constants.LimitKind.NotionDatabaseNumber,
				9L
			},
			{
				Constants.LimitKind.GoogleCalendarNumber,
				3L
			},
			{
				Constants.LimitKind.GoogleConnectNumber,
				9L
			},
			{
				Constants.LimitKind.CalendarBindNumber,
				3L
			},
			{
				Constants.LimitKind.UrlCalendarNumber,
				5L
			},
			{
				Constants.LimitKind.HolidayNumber,
				10L
			}
		};
	}

	public static bool CheckCalendarCount(string kind)
	{
		if (kind == "URL")
		{
			List<CalendarSubscribeProfileModel> subscribeCalendars = CacheManager.GetSubscribeCalendars();
			long limitByKey = GetLimitByKey(Constants.LimitKind.UrlCalendarNumber);
			if (subscribeCalendars.Count >= limitByKey)
			{
				CustomerDialog customerDialog = new CustomerDialog(Utils.GetString("LimitTips"), Utils.GetString("UrlToMany"), MessageBoxButton.OK);
				customerDialog.Owner = App.Window;
				customerDialog.ShowDialog();
				return false;
			}
			return true;
		}
		List<BindCalendarAccountModel> bindCalendarAccounts = CacheManager.GetBindCalendarAccounts();
		int num = 0;
		long num2 = 3L;
		switch (kind)
		{
		case "CalDAV":
			num = bindCalendarAccounts.Count((BindCalendarAccountModel item) => item.Kind == "caldav");
			break;
		case "Google":
			num = bindCalendarAccounts.Count((BindCalendarAccountModel item) => item.Site == "google");
			num2 = GetLimitByKey(Constants.LimitKind.GoogleCalendarNumber);
			break;
		case "Notion":
			num = bindCalendarAccounts.Count((BindCalendarAccountModel item) => item.Site == "notion");
			num2 = GetLimitByKey(Constants.LimitKind.NotionWorkspaceNumber);
			break;
		case "Outlook":
			num = bindCalendarAccounts.Count((BindCalendarAccountModel item) => item.Site == "outlook");
			break;
		case "Exchange":
			num = bindCalendarAccounts.Count((BindCalendarAccountModel item) => item.Kind == "exchange");
			break;
		case "iCloud":
			num = bindCalendarAccounts.Count((BindCalendarAccountModel item) => item.Kind == "icloud");
			break;
		}
		if (num >= num2)
		{
			new CustomerDialog(Utils.GetString("LimitTips"), string.Format(Utils.GetString("CalendarBindExceed"), kind), MessageBoxButton.OK).ShowDialog();
			return false;
		}
		return true;
	}
}
