using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using TickTickDao;
using TickTickModels;
using ticktick_WPF.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Files;
using ticktick_WPF.Util.Sync.ClosedLoader;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.Views.AI;

namespace ticktick_WPF.Dal;

public static class TickDbHelper
{
	public static readonly IReadOnlyDictionary<string, List<string>> SyncSortOrderModelIndexes = new Dictionary<string, List<string>>
	{
		{
			"SyncSortOrderModel_UserId_SortOrderType_Deleted",
			new List<string> { "UserId", "SortOrderType", "Deleted" }
		},
		{
			"SyncSortOrderModel_UserId_SyncStatus",
			new List<string> { "UserId", "SyncStatus" }
		},
		{
			"SyncSortOrderModel_UserId_SortOrderType_GroupId_EntityId_Type",
			new List<string> { "UserId", "SortOrderType", "GroupId", "EntityId", "Type" }
		},
		{
			"SyncSortOrderModel_UserId_GroupId_SortOrderType_Deleted",
			new List<string> { "UserId", "GroupId", "SortOrderType", "Deleted" }
		}
	};

	public static async Task<bool?> CreateDbAsync(bool logFirst = true)
	{
		bool firstInstall = false;
		string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string path = dir + "\\Tick_Tick";
		bool flag = Directory.Exists(path);
		if (!flag)
		{
			Directory.CreateDirectory(path);
		}
		if (logFirst)
		{
			firstInstall = !flag;
		}
		if (!Directory.Exists(dir + "\\Tick_Tick\\Image"))
		{
			Directory.CreateDirectory(dir + "\\Tick_Tick\\Image");
		}
		Connection.DbConnection = new SQLiteAsyncConnection(dir + "\\Tick_Tick\\TickTick.db");
		await Connection.DbConnection.ExecuteAsync("PRAGMA journal_mode=WAL");
		string version = Utils.GetVersion();
		if (File.Exists(dir + "\\Tick_Tick\\dbversion" + version + ".txt"))
		{
			return null;
		}
		await App.Connection.CreateTableAsync<UserModel>();
		await App.Connection.CreateTableAsync<UserInfoModel>();
		await App.Connection.CreateTableAsync<SettingsModel>();
		await App.Connection.CreateTableAsync<AIPinStorageRow>();
		await App.Connection.CreateTableAsync<MatrixProfileModel>();
		await App.Connection.CreateTableAsync<MatrixProfileQuadrantModel>();
		await App.Connection.CreateTableAsync<ProjectModel>();
		await App.Connection.CreateTableAsync<ProjectGroupModel>();
		await App.Connection.CreateTableAsync<TaskModel>();
		await App.Connection.CreateTableAsync<TaskDetailItemModel>();
		await App.Connection.CreateTableAsync<TaskReminderModel>();
		await App.Connection.CreateTableAsync<UserPublicProfilesModel>();
		await App.Connection.CreateTableAsync<TempDataModel>();
		await App.Connection.CreateTableAsync<LimitsModel>();
		await App.Connection.CreateTableAsync<SyncStatusModel>();
		await App.Connection.CreateTableAsync<TaskSyncedJsonModel>();
		await App.Connection.CreateTableAsync<SectionStatusModel>();
		await App.Connection.CreateTableAsync<AttachmentModel>();
		await App.Connection.CreateTableAsync<TagSortTypeModel>();
		await App.Connection.CreateTableAsync<CommentModel>();
		await App.Connection.CreateTableAsync<FilterModel>();
		await App.Connection.CreateTableAsync<HolidayModel>();
		await App.Connection.CreateTableAsync<PomodoroSummaryModel>();
		await App.Connection.CreateTableAsync<PomodoroModel>();
		await App.Connection.CreateTableAsync<WidgetSettingModel>();
		await App.Connection.CreateTableAsync<CalendarWidgetModel>();
		await App.Connection.CreateTableAsync<TagModel>();
		await App.Connection.CreateTableAsync<CalendarSubscribeProfileModel>();
		await App.Connection.CreateTableAsync<CalendarEventModel>();
		await App.Connection.CreateTableAsync<BindCalendarAccountModel>();
		await App.Connection.CreateTableAsync<BindCalendarModel>();
		await App.Connection.CreateTableAsync<AppLockModel>();
		await App.Connection.CreateTableAsync<TaskDefaultModel>();
		await App.Connection.CreateTableAsync<LocationModel>();
		await App.Connection.CreateTableAsync<AttendModel>();
		await App.Connection.CreateTableAsync<ColumnModel>();
		await App.Connection.CreateTableAsync<TaskSortOrderInDateModel>();
		await App.Connection.CreateTableAsync<TaskSortOrderInPriorityModel>();
		await App.Connection.CreateTableAsync<TaskSortOrderInProjectModel>();
		await App.Connection.CreateTableAsync<TeamModel>();
		await App.Connection.CreateTableAsync<SearchHistoryModel>();
		await App.Connection.CreateTableAsync<TaskTemplateModel>();
		await App.Connection.CreateTableAsync<ProjectTemplatesModel>();
		await App.Connection.CreateTableAsync<ArchivedEventModel>();
		await App.Connection.CreateTableAsync<ArrangeSectionStatusModel>();
		await App.Connection.CreateTableAsync<HabitModel>();
		await App.Connection.CreateTableAsync<HabitCheckInModel>();
		await App.Connection.CreateTableAsync<HabitRecordModel>();
		await App.Connection.CreateTableAsync<SkipHabitModel>();
		await App.Connection.CreateTableAsync<CommonSettings>();
		await App.Connection.CreateTableAsync<ProjectSyncedJsonModel>();
		await App.Connection.CreateTableAsync<PomoTask>();
		await App.Connection.CreateTableAsync<FilterSyncedJsonModel>();
		await App.Connection.CreateTableAsync<TagSyncedJsonModel>();
		await App.Connection.CreateTableAsync<ProjectGroupSyncedJsonModel>();
		await App.Connection.CreateTableAsync<ShowCountDownModel>();
		await App.Connection.CreateTableAsync<ClosedLoadStatus>();
		await App.Connection.CreateTableAsync<HabitSectionModel>();
		await App.Connection.CreateTableAsync<SyncSortOrderModel>();
		await App.Connection.CreateTableAsync<TaskCompletionRate>();
		await App.Connection.CreateTableAsync<CourseScheduleModel>();
		await App.Connection.CreateTableAsync<CourseModel>();
		await App.Connection.CreateTableAsync<TaskStickyModel>();
		await App.Connection.CreateTableAsync<FocusOptionModel>();
		await App.Connection.CreateTableAsync<WindowModel>();
		await App.Connection.CreateTableAsync<TimerModel>();
		await App.Connection.CreateTableAsync<TimerStatisticsModel>();
		await App.Connection.CreateTableAsync<ReminderDelayModel>();
		await App.Connection.CreateTableAsync<ReminderTimeModel>();
		await App.Connection.CreateTableAsync<CalendarConnectInfo>();
		await App.Connection.CreateTableAsync<TaskGoogleEventInfo>();
		await App.Connection.CreateTableAsync<CountdownModel>();
		await App.Connection.CreateTableAsync<CountdownWidgetModel>();
		await App.Connection.CreateTableAsync<HabitWidgetModel>();
		await App.Connection.CreateTableAsync<HabitWeekWidgetModel>();
		await App.Connection.CreateTableAsync<AudioAttachmentAIResultModel>();
		await SetTaskModelIndex();
		await SetOtherModelIndex();
		FileUtils.ResetDbVersionFile();
		return firstInstall;
	}

	private static async Task SetTaskModelIndex()
	{
		await AddModelIndex("TaskModel", new Dictionary<string, List<string>> { 
		{
			"TaskModel_status_deleted",
			new List<string> { "status", "deleted" }
		} });
		await RemoveIndex("TaskModel_userId");
	}

	private static async Task SetOtherModelIndex()
	{
		await AddModelIndex("SyncSortOrderModel", SyncSortOrderModelIndexes);
		await AddModelIndex("TaskDetailItemModel", new Dictionary<string, List<string>> { 
		{
			"TaskDetailItemModel_status_TaskServerId_startDate",
			new List<string> { "status", "TaskServerId", "startDate" }
		} });
		await AddModelIndex("MatrixProfileModel", new Dictionary<string, List<string>>
		{
			{
				"MatrixProfileModel_userId_sid",
				new List<string> { "userId", "sid" }
			},
			{
				"MatrixProfileModel_userId_deleted_sortOrder",
				new List<string> { "userId", "deleted", "sortOrder" }
			}
		});
		await AddModelIndex("MatrixProfileQuadrantModel", new Dictionary<string, List<string>>
		{
			{
				"MatrixProfileQuadrantModel_userId_matrixSid",
				new List<string> { "userId", "matrixSid" }
			},
			{
				"MatrixProfileQuadrantModel_userId_matrixSid_quadrant",
				new List<string> { "userId", "matrixSid", "quadrant" }
			}
		});
		await AddModelIndex("HabitCheckInModel", new Dictionary<string, List<string>> { 
		{
			"HabitCheckInModel_HabitId_CheckinStamp",
			new List<string> { "HabitId", "CheckinStamp" }
		} });
	}

	private static async Task AddModelIndex(string tableName, IReadOnlyDictionary<string, List<string>> indexDict)
	{
		foreach (KeyValuePair<string, List<string>> item in indexDict)
		{
			string key = item.Key;
			List<string> value = item.Value;
			if (value != null && value.Count != 0)
			{
				string text = value.Join(",");
				await App.Connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS " + key + " ON " + tableName + "(" + text + ")");
			}
		}
	}

	private static async Task RemoveIndex(string indexName)
	{
		await App.Connection.ExecuteAsync("DROP INDEX IF EXISTS " + indexName);
	}
}
