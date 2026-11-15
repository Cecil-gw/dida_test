using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices;
using ticktick_WPF.Util.Attachment;
using ticktick_WPF.Util.Sync.Model;
using ticktick_WPF.Util.Twitter;
using ticktick_WPF.ViewModels;
using ticktick_WPF.Views;
using ticktick_WPF.Views.Config;
using ticktick_WPF.Views.Pomo;
using ticktick_WPF.Views.Undo;

namespace ticktick_WPF.Util.Sync;

public class SyncService
{
	private const double TaskAnnoyingAlertVersion = 8080.0;

	private static readonly SemaphoreLocker SyncLock = new SemaphoreLocker();

	public static bool NeedRecommit;

	private readonly SyncResult _syncResult = new SyncResult();

	private EventBatchHandler _eventBatchHandler;

	private FilterBatchHandler _filterBatchHandler;

	private ProjectBatchHandler _projectBatchHandler;

	private ProjectGroupBatchHandler _projectGroupBatchHandler;

	private TaskOrderBatchHandler _sortOrderBatchHandler;

	private SyncOrderBatchHandler _syncOrderBatchHandler;

	private TagBatchHandler _tagBatchHandler;

	private TaskBatchHandler _taskBatchHandler;

	private bool _needSync;

	private bool _matrixProfilesPulledThisSync;

	private bool _matrixPreferencePulledThisSync;

	private LogModel _syncLog;

	private bool _pullingOthers;

	private static readonly BlockingList<string> TaskPullingProjectIds = new BlockingList<string>();

	public event EventHandler<SyncResult> SyncFinished;

	public event EventHandler<SyncResult> SyncStart;

	public void Init()
	{
		InitBatchHandler();
	}

	private void InitBatchHandler()
	{
		string loginUserId = LocalSettings.Settings.LoginUserId;
		_projectGroupBatchHandler = new ProjectGroupBatchHandler(loginUserId, _syncResult);
		_projectBatchHandler = new ProjectBatchHandler(loginUserId, _syncResult);
		_taskBatchHandler = new TaskBatchHandler(loginUserId, _syncResult);
		_sortOrderBatchHandler = new TaskOrderBatchHandler(loginUserId, _syncResult);
		_syncOrderBatchHandler = new SyncOrderBatchHandler(loginUserId, _syncResult);
		_filterBatchHandler = new FilterBatchHandler(loginUserId, _syncResult);
		_tagBatchHandler = new TagBatchHandler(loginUserId, _syncResult);
		_eventBatchHandler = new EventBatchHandler(loginUserId, _syncResult);
	}

	public async Task DoSync(int type, string projectId = "")
	{
		if (type == 0)
		{
			_needSync = true;
		}
		await SyncLock.LockAsync(async delegate
		{
			if (type == 0)
			{
				if (!_needSync)
				{
					return;
				}
				_needSync = false;
			}
			try
			{
				this.SyncStart?.Invoke(this, null);
				_syncResult.Clear();
				await SyncData(type, projectId);
			}
			finally
			{
				SyncResult syncResult = _syncResult.Copy();
				syncResult.SyncType = type;
				this.SyncFinished?.Invoke(this, syncResult);
			}
		});
	}

	public static async Task PullPreferenceChangesAsync()
	{
		await SyncLock.LockAsync(async delegate
		{
			_ = 2;
			try
			{
				if (await SettingsHelper.PullRemotePreference(0L) != null)
				{
					List<MatrixProfileSyncModel> list = await Communicator.GetMatrixProfiles();
					bool flag = list != null;
					if (flag)
					{
						flag = await MatrixProfileDao.MergeFromServerAsync(list);
					}
					if (flag)
					{
						DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
					}
				}
			}
			catch (Exception ex)
			{
				UtilLog.Warn("SyncService.PullPreferenceChangesAsync " + ex);
			}
		});
	}

	private static void SetSyncPoint()
	{
		LocalSettings.Settings.SyncPoint = DateTime.Now.Ticks;
	}

	private async Task SyncData(int type, string projectId = "")
	{
		_syncLog = new LogModel(TicketLogUtils.GetLogString("Start SyncData"));
		try
		{
			_ = 6;
			try
			{
				NeedRecommit = false;
				_matrixProfilesPulledThisSync = false;
				_matrixPreferencePulledThisSync = false;
				await PullOtherData(type);
				await PullColumns(projectId);
				await Pull(type);
				LogModel syncLog = _syncLog;
				syncLog.Log = syncLog.Log + "\r\n\t" + TicketLogUtils.GetLogString("big sync: will post data");
				await Commit(type);
				if (NeedRecommit)
				{
					await Commit(type);
				}
				_syncLog.Log += "\r\n\tbig sync: did post data";
				UtilLog.Info(_syncLog.Log);
				await HandleDirtyData(type);
				await TryUploadAttachment();
			}
			catch (Exception ex)
			{
				UtilLog.Error(ex);
			}
		}
		finally
		{
			await TaskGoogleEventInfoService.SyncForCurrentConnectionsAsync();
		}
	}

	private async Task TryUploadAttachment()
	{
		await AttachmentUploadUtils.UploadAllFiles();
	}

	private async Task HandleDirtyData(int type)
	{
		if (type == 1)
		{
			try
			{
				await TaskDao.HandleDirtyData();
			}
			catch (Exception)
			{
			}
		}
	}

	private async Task PullColumns(string projectId)
	{
		if (string.IsNullOrEmpty(projectId))
		{
			await PullColumns();
		}
		else
		{
			await PullProjectColumns(projectId);
		}
	}

	private async Task PullColumns()
	{
		await PullColumns(await UserDao.GetColumnCheckPoint(Utils.GetCurrentUserIdInt().ToString()));
	}

	private async Task PullColumns(long checkPoint)
	{
		SyncColumnBean syncColumnBean = await Communicator.CheckRemoteColumnChanged(checkPoint);
		if (syncColumnBean != null)
		{
			_syncLog.Log += $"\r\n\tpullColumns u{syncColumnBean.Update?.Count} a{syncColumnBean.Add?.Count} d{syncColumnBean.Deleted?.Count}";
			await ColumnBatchHandler.MergeWithServer(syncColumnBean, _syncLog);
			await UserDao.SaveColumnCheckPoint(Utils.GetCurrentUserIdInt().ToString(), Utils.GetNowTimeStampInMills());
		}
	}

	private async Task PullProjectColumns(string projectId)
	{
		if (!string.IsNullOrEmpty(projectId))
		{
			SyncResult syncResult = _syncResult;
			syncResult.ColumnChanged = await ColumnBatchHandler.MergeWithServer(projectId);
		}
	}

	private async Task PullOtherData(int type)
	{
		if (!_pullingOthers)
		{
			_pullingOthers = true;
			if (type == 1)
			{
				TickFocusManager.GetRemotePomos();
				TimerSyncService.PullRemoteTimers();
				PullRemoteCalendars();
				EventArchiveSyncService.PullArchivedModels();
				CourseArchiveSyncService.PullArchivedModels();
				CountdownSyncService.SyncCountdowns();
				await Task.WhenAll(SyncUserSettings(), PullNotificationCount(), LimitCache.InitLimitCache(), AvatarHelper.GetAllProjectAvatars(), SyncPomoSettings(), TryPullHabitCheckIns(), ScheduleService.GetRemoteSchedules());
			}
			_pullingOthers = false;
		}
	}

	private static async Task TryPullHabitCheckIns()
	{
		if (await UserDao.GetUserSyncCheckPoint(LocalSettings.Settings.LoginUserId) > 0)
		{
			await HabitSyncService.PullHabits(91);
			await HabitSectionsSyncService.PullHabitSections();
		}
	}

	private static async Task<bool> PullRemoteTeams()
	{
		return await TeamService.PullTeams();
	}

	private static async Task SyncUserSettings()
	{
		await HabitService.PullRemoteConfig().ContinueWith(UtilRun.LogTask);
		await SettingsHelper.PullRemoteSettings().ContinueWith(UtilRun.LogTask);
	}

	private static async Task PullRemoteCalendars()
	{
		_ = 1;
		try
		{
			await CalendarService.PullSubscribeCalendars();
			await CalendarService.PullAccountCalendarsAndEvents();
			CalendarService.CheckExpiredAccounts();
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	private static async Task SyncPomoSettings()
	{
		await PomoSettings.LoadRemoteSettings();
	}

	private async Task PullNotificationCount()
	{
		NotificationUnreadCount notificationUnreadCount = await Communicator.GetNotificationCount();
		if (notificationUnreadCount != null && (notificationUnreadCount.Activity > 0 || notificationUnreadCount.Notification > 0))
		{
			_syncResult.NotificationCount = notificationUnreadCount;
		}
		else
		{
			_syncResult.NotificationCount = new NotificationUnreadCount(0, 0);
		}
	}

	public void InitNotification()
	{
		_syncResult.InitNotification();
	}

	private async Task Commit(int type)
	{
		SetSyncPoint();
		try
		{
			await ProjectBatchHandler.CommitToServer(delete: false, _syncLog);
			await ProjectGroupBatchHandler.CommitToServer(_syncLog);
			await CommitMoveColumnProject(checkReCommit: true);
			await CommitMoveOrRestoreProject(2);
			await CommitMoveOrRestoreProject(7);
			await ProjectBatchHandler.CommitToServer(delete: true, _syncLog);
			await ColumnBatchHandler.CommitToServer(_syncLog);
			EventArchiveSyncService.PushLocalArchivedModels();
			CourseArchiveSyncService.PushLocalArchivedModels();
			await ReminderDelayService.UploadDelayModels();
			await CommitFilter();
			await CommitMatrixProfiles();
			await CommitTag();
			await CommitTask();
			await CommitAttachment();
			await CommitTemplate();
			await CommitClearTrash();
			await CommitTaskOrder();
			await CommitSyncOrder();
			await CommitTaskParent();
			await CommitPomodoros(type);
			await CommitEvents();
			await TimerSyncService.PostTimers();
			await HabitSyncService.CommitHabitAndCheckIns();
			List<CountdownModel> list = await CountdownDao.GetNeedPostCountdowns();
			if (list != null && list.Count > 0)
			{
				await CountdownSyncService.SyncCountdowns();
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}

	private async Task CommitAttachment()
	{
		List<AttachmentModel> list = await AttachmentDao.GetNeedUpdateAttachments();
		if (list == null || list.Count == 0)
		{
			return;
		}
		Dictionary<string, List<AttachmentModel>> dictionary = new Dictionary<string, List<AttachmentModel>>();
		foreach (AttachmentModel item in list)
		{
			if (!string.IsNullOrEmpty(item.taskId))
			{
				if (dictionary.ContainsKey(item.taskId))
				{
					dictionary[item.taskId].Add(item);
					continue;
				}
				dictionary[item.taskId] = new List<AttachmentModel> { item };
			}
		}
		SyncTaskBean addBean = new SyncTaskBean();
		SyncTaskBean updateBean = new SyncTaskBean();
		foreach (KeyValuePair<string, List<AttachmentModel>> taskAttachments in dictionary)
		{
			TaskModel taskModel = await TaskDao.GetTaskById(taskAttachments.Key);
			if (taskModel != null && taskModel.deleted == 0)
			{
				AttachmentModel[] array = taskAttachments.Value.Where((AttachmentModel a) => a.id != a.refId && a.sync_status == 0.ToString()).ToArray();
				AttachmentModel[] array2 = taskAttachments.Value.Where((AttachmentModel a) => a.sync_status == 1.ToString()).ToArray();
				if (array.Length != 0)
				{
					addBean.AddAttachments.Add(new TaskModel
					{
						Attachments = array,
						id = taskModel.id,
						projectId = taskModel.projectId
					});
				}
				if (array2.Length != 0)
				{
					updateBean.UpdateAttachments.Add(new TaskModel
					{
						Attachments = array2,
						id = taskModel.id,
						projectId = taskModel.projectId
					});
				}
			}
		}
		if (addBean.AddAttachments.Count > 0)
		{
			BatchUpdateResult updatedResult = await Communicator.BatchUpdateTask(addBean);
			await AttachmentDao.HandleCommitResult(addBean.AddAttachments, updatedResult);
		}
		if (updateBean.UpdateAttachments.Count > 0)
		{
			BatchUpdateResult updatedResult2 = await Communicator.BatchUpdateTask(updateBean);
			await AttachmentDao.HandleCommitResult(updateBean.UpdateAttachments, updatedResult2);
		}
	}

	private async Task CommitTaskParent()
	{
		List<string> addIds = (await SyncStatusDao.GetSyncStatusByType(4)).Select((SyncStatusModel s) => s.EntityId).ToList();
		List<string> undoIds = CloseUndoHandler.GetAffectedTaskIds();
		List<SyncStatusModel> syncStatus = (await SyncStatusDao.GetSyncStatusByType(16)).Where((SyncStatusModel s) => !addIds.Contains(s.EntityId) && !undoIds.Contains(s.EntityId)).ToList();
		if (syncStatus.Count <= 0)
		{
			return;
		}
		List<SyncTaskParentModel> taskParentModels = new List<SyncTaskParentModel>();
		foreach (SyncStatusModel status in syncStatus)
		{
			TaskModel task = await TaskDao.GetThinTaskById(status.EntityId);
			if (task != null)
			{
				if (task.parentId == task.id)
				{
					task.parentId = "";
					await TaskService.UpdateTaskParent(task);
				}
				if (task.deleted == 0 && Utils.IsEqualString(task.parentId, status.OldParentId))
				{
					await SyncStatusDao.DeleteSyncStatus(status.EntityId, status.Type);
					continue;
				}
				SyncTaskParentModel item = new SyncTaskParentModel
				{
					taskId = task.id,
					parentId = ((task.deleted != 0) ? "" : task.parentId),
					projectId = task.projectId,
					oldParentId = status.OldParentId
				};
				taskParentModels.Add(item);
			}
			else
			{
				await SyncStatusDao.DeleteSyncStatus(status.EntityId, status.Type);
			}
		}
		if (taskParentModels.Count <= 0)
		{
			return;
		}
		await ProjectBatchHandler.CommitToServer(delete: true, _syncLog);
		int batchSize = 1000;
		for (int i = 0; i < taskParentModels.Count; i += batchSize)
		{
			int count = Math.Min(batchSize, taskParentModels.Count - i);
			SyncTaskParentRes result = await Communicator.BatchPushTaskParent(taskParentModels.GetRange(i, count));
			List<SyncStatusModel> extraStatus = (await SyncStatusDao.GetSyncStatusByType(16)).Where((SyncStatusModel s) => !taskParentModels.Exists((SyncTaskParentModel model) => model.taskId == s.EntityId)).ToList();
			Dictionary<string, SyncTaskParentEtag> id2etag = result.Id2etag;
			if (id2etag != null && id2etag.Count > 0)
			{
				foreach (KeyValuePair<string, SyncTaskParentEtag> kv in result.Id2etag)
				{
					TaskModel task2 = await TaskDao.GetTaskById(kv.Key);
					if (task2 != null && !extraStatus.Exists((SyncStatusModel s) => s.EntityId == task2.id))
					{
						task2.etag = kv.Value.etag;
						task2.parentId = kv.Value.parentId;
						TaskModel taskModel = task2;
						string[] childIds = kv.Value.childIds;
						taskModel.childrenString = ((childIds != null && childIds.Length != 0) ? JsonConvert.SerializeObject(kv.Value.childIds) : null);
						await TaskService.UpdateTaskParent(task2);
					}
				}
				foreach (SyncStatusModel item2 in syncStatus.Where((SyncStatusModel syncStatusModel) => result.Id2etag.ContainsKey(syncStatusModel.EntityId)))
				{
					await SyncStatusDao.DeleteSyncStatus(item2.EntityId, item2.Type);
				}
			}
			Dictionary<string, string> id2error = result.Id2error;
			if (id2error == null || id2error.Count <= 0)
			{
				continue;
			}
			foreach (SyncStatusModel item3 in syncStatus.Where((SyncStatusModel syncStatusModel) => result.Id2error.ContainsKey(syncStatusModel.EntityId)))
			{
				switch (result.Id2error[item3.EntityId])
				{
				case "NO_PROJECT_PERMISSION":
				case "EXISTED":
				case "NOT_EXISTED":
					await SyncStatusDao.DeleteSyncStatus(item3.EntityId, item3.Type, checkRecommit: false);
					break;
				}
			}
		}
	}

	private static async Task CommitTemplate()
	{
		List<SyncStatusModel> localStatus = await (from m in App.Connection.Table<SyncStatusModel>()
			where m.Type >= 13 && m.Type <= 15 && m.UserId == LocalSettings.Settings.LoginUserId
			select m).ToListAsync();
		List<TaskTemplateModel> source = await TemplateDao.GetLocalTemplate();
		if (localStatus == null || localStatus.Count <= 0)
		{
			return;
		}
		TaskTemplateSyncBean taskTemplateSyncBean = new TaskTemplateSyncBean();
		TaskTemplateSyncBean noteTemplateSyncBean = new TaskTemplateSyncBean();
		foreach (SyncStatusModel status in localStatus)
		{
			TaskTemplateModel taskTemplateModel = source.FirstOrDefault((TaskTemplateModel t) => t.Id == status.EntityId);
			if (taskTemplateModel == null)
			{
				continue;
			}
			switch (status.Type)
			{
			case 13:
				if (taskTemplateModel.Kind == TemplateKind.Note.ToString())
				{
					noteTemplateSyncBean.Add.Add(taskTemplateModel);
				}
				else
				{
					taskTemplateSyncBean.Add.Add(taskTemplateModel);
				}
				break;
			case 14:
				if (taskTemplateModel.Kind == TemplateKind.Note.ToString())
				{
					noteTemplateSyncBean.Update.Add(taskTemplateModel);
				}
				else
				{
					taskTemplateSyncBean.Update.Add(taskTemplateModel);
				}
				break;
			case 15:
				if (taskTemplateModel.Kind == TemplateKind.Note.ToString())
				{
					noteTemplateSyncBean.Delete.Add(taskTemplateModel.Id);
				}
				else
				{
					taskTemplateSyncBean.Delete.Add(taskTemplateModel.Id);
				}
				break;
			}
		}
		BatchUpdateResult result;
		if (!taskTemplateSyncBean.IsEmpty())
		{
			result = await Communicator.PushTemplates(taskTemplateSyncBean);
			if (result != null)
			{
				await TemplateDao.SaveCommitResultBackToDb(result.Id2etag);
				foreach (SyncStatusModel item in localStatus)
				{
					if (result.Id2etag.Keys.Contains(item.EntityId))
					{
						await App.Connection.DeleteAsync(item);
					}
				}
			}
		}
		if (noteTemplateSyncBean.IsEmpty())
		{
			return;
		}
		result = await Communicator.PushTemplates(noteTemplateSyncBean, isNote: true);
		if (result == null)
		{
			return;
		}
		await TemplateDao.SaveCommitResultBackToDb(result.Id2etag);
		foreach (SyncStatusModel item2 in localStatus)
		{
			if (result.Id2etag.Keys.Contains(item2.EntityId))
			{
				await App.Connection.DeleteAsync(item2);
			}
		}
	}

	private static async Task CommitTaskOrder()
	{
		SyncTaskOrderBean taskOrderBean = await TaskOrderBatchHandler.CreateCommitBean();
		if (!InvalidSyncTaskOrderBean(taskOrderBean))
		{
			long lastPostPoint = DateTime.Now.Ticks;
			BatchTaskOrderUpdateResult batchTaskOrderUpdateResult = await Communicator.BatchUpdateTaskOrder(taskOrderBean);
			if (batchTaskOrderUpdateResult != null)
			{
				await TaskOrderBatchHandler.HandleCommitResult(batchTaskOrderUpdateResult, lastPostPoint);
			}
		}
	}

	private async Task CommitSyncOrder()
	{
		SyncOrderBean syncOrderBean = await SyncOrderBatchHandler.CreateCommitBean();
		if (syncOrderBean.OrderByType == null || syncOrderBean.OrderByType.Count <= 0)
		{
			return;
		}
		SyncOrderBean taskOrderBean = syncOrderBean.GetTaskOrderBean();
		if (taskOrderBean.OrderByType.Count > 0)
		{
			BatchSyncOrderUpdateResult batchSyncOrderUpdateResult = await Communicator.BatchUpdateTaskSyncOrder(taskOrderBean);
			if (batchSyncOrderUpdateResult != null)
			{
				await SyncOrderBatchHandler.HandleCommitResult(batchSyncOrderUpdateResult);
			}
		}
		SyncOrderBean otherOrderBean = syncOrderBean.GetOtherOrderBean();
		if (otherOrderBean.OrderByType.Count > 0)
		{
			BatchSyncOrderUpdateResult batchSyncOrderUpdateResult2 = await Communicator.BatchUpdateSyncOrder(otherOrderBean);
			if (batchSyncOrderUpdateResult2 != null)
			{
				await SyncOrderBatchHandler.HandleCommitResult(batchSyncOrderUpdateResult2);
			}
		}
	}

	private static bool InvalidSyncTaskOrderBean(SyncTaskOrderBean taskOrderBean)
	{
		if ((taskOrderBean?.taskOrderByDate == null || taskOrderBean.taskOrderByDate.Count == 0) && (taskOrderBean?.taskOrderByPriority == null || taskOrderBean.taskOrderByPriority.Count == 0))
		{
			if (taskOrderBean?.taskOrderByProject != null)
			{
				return taskOrderBean.taskOrderByProject.Count == 0;
			}
			return true;
		}
		return false;
	}

	private async Task Pull(int type)
	{
		string lastVersion = LocalSettings.Settings.ExtraSettings.LastVersion;
		long lastCheckPoint = await UserDao.GetUserSyncCheckPoint(LocalSettings.Settings.LoginUserId);
		if (lastCheckPoint < 0)
		{
			lastCheckPoint = 0L;
		}
		bool ignoreEtag = lastVersion != Utils.GetVersion();
		long point = (NeedPullTaskAnnoyingAlert(lastVersion) ? 0 : lastCheckPoint);
		LogModel syncLog = _syncLog;
		syncLog.Log = syncLog.Log + "\r\n\t" + TicketLogUtils.GetLogString("big sync: will batch check \\" + point);
		SyncBean syncBean = await Communicator.BatchCheck(point, lastVersion);
		if (syncBean != null && !syncBean.IsEmpty())
		{
			_syncLog.Log += "\r\n\tbig sync: will handle batch check result";
			try
			{
				await TryPullRemoteTeams(type, lastCheckPoint == 0);
				await _projectGroupBatchHandler.MergeWithServer(syncBean.projectGroups, _syncLog);
				await _projectBatchHandler.MergeWithServer(syncBean.projectProfiles, _syncLog, lastCheckPoint);
				await _filterBatchHandler.MergeWithServer(syncBean.filters, _syncLog);
				await _tagBatchHandler.MergeWithServer(syncBean.tags, _syncLog);
				await _taskBatchHandler.MergeWithServer(syncBean.syncTaskBean, type, _syncLog, ignoreEtag);
				await _sortOrderBatchHandler.MergeWithServer(syncBean.syncTaskOrderBean);
				await _syncOrderBatchHandler.MergeWithServer(syncBean.syncOrderBean);
				await _syncOrderBatchHandler.MergeWithServer(syncBean.syncOrderBeanV3);
				await TryPullRemoteHabits(lastCheckPoint == 0);
				await TaskBatchHandler.HandleRepeatCompletedTasks(syncBean.syncTaskBean);
				await ReminderDelayService.MergeRemoteDelayModels(syncBean.remindChanges);
				bool matrixProfilesChanged = false;
				if (await PullMatrixPreference())
				{
					matrixProfilesChanged = await MatrixProfileDao.MergeFromServerAsync(syncBean.matrixProfiles, null, authoritative: false);
				}
				matrixProfilesChanged = await PullMatrixProfiles() || matrixProfilesChanged;
				NotifyMatrixProfilesChanged(matrixProfilesChanged);
				await SaveCheckPoint(syncBean.checkPoint);
				await SaveChecks(syncBean.checks);
			}
			catch (Exception ex)
			{
				LogModel syncLog2 = _syncLog;
				syncLog2.Log = syncLog2.Log + "\r\n\t" + ExceptionUtils.BuildExceptionMessage(ex);
			}
			_syncLog.Log += "\r\n\tbig sync: did handle batch check result ";
		}
		else if (syncBean != null)
		{
			NotifyMatrixProfilesChanged(await PullMatrixProfiles());
			await SaveCheckPoint(syncBean.checkPoint);
		}
		if (lastCheckPoint == 0L)
		{
			ClosedListSyncService.PullCompletedTaskAtFirstLogin();
		}
	}

	private static bool NeedPullTaskAnnoyingAlert(string lastVersion)
	{
		if (Utils.VersionNumToDouble() < 8080.0)
		{
			return false;
		}
		if (double.TryParse(lastVersion?.Replace(".", ""), out var result))
		{
			if (result > 0.0)
			{
				return result < 8080.0;
			}
			return false;
		}
		return false;
	}

	private async Task SaveChecks(string checks)
	{
		if (string.IsNullOrEmpty(checks))
		{
			return;
		}
		try
		{
			if (!JObject.Parse(checks).TryGetValue("checks", out JToken value) || !(value is JArray jArray))
			{
				return;
			}
			foreach (JToken item in jArray)
			{
				if (!(item is JObject jObject) || !jObject.TryGetValue("action", out JToken value2) || !(value2?.ToString() == "task.move") || !jObject.TryGetValue("data", out JToken value3))
				{
					continue;
				}
				try
				{
					MoveCheckModel moveCheckModel = JsonConvert.DeserializeObject<MoveCheckModel>(value3.ToString());
					if (moveCheckModel == null)
					{
						continue;
					}
					TaskBaseViewModel taskById = TaskCache.GetTaskById(moveCheckModel.id);
					if (taskById != null && taskById.ProjectId != moveCheckModel.projectId)
					{
						ProjectModel projectById = CacheManager.GetProjectById(moveCheckModel.projectId);
						if (projectById == null || projectById.IsDeleted)
						{
							TaskDao.DeleteTaskInDb(moveCheckModel.id);
						}
					}
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static async Task TryPullRemoteHabits(bool first)
	{
		if (first)
		{
			await HabitSyncService.SyncHabitsOnLogin();
			HabitSectionsSyncService.SyncSections();
			CountdownSyncService.SyncCountdowns();
		}
	}

	private async Task TryPullRemoteTeams(int type, bool first)
	{
		bool teamChanged = await PullRemoteTeams();
		_syncResult.TeamChanged = teamChanged;
	}

	public async Task PullProjectTasks(string projectId)
	{
		if (TaskPullingProjectIds.Contains(projectId))
		{
			return;
		}
		TaskPullingProjectIds.Add(projectId);
		try
		{
			Dictionary<string, ObservableCollection<TaskModel>> serverTasksDict = new Dictionary<string, ObservableCollection<TaskModel>>();
			ObservableCollection<TaskModel> observableCollection = await Communicator.PullServerTasksByProjectId(projectId);
			if (observableCollection != null)
			{
				serverTasksDict.Add(projectId, observableCollection);
				await _taskBatchHandler.MergeTasksOfOpenedProjects(serverTasksDict);
			}
		}
		finally
		{
			TaskPullingProjectIds.Remove(projectId);
		}
	}

	private static async Task SaveCheckPoint(long checkPoint)
	{
		await UserDao.SaverUserSyncCheckPoint(LocalSettings.Settings.LoginUserId, checkPoint);
		LocalSettings.Settings.ExtraSettings.LastVersion = Utils.GetVersion();
		LocalSettings.Settings.Save(force: true);
	}

	private static async Task CommitPomodoros(int type)
	{
		await FocusOptionUploader.SaveOption(null, type == 1, resetLocalPieces: true);
		await PomoSyncService.CommitPomodoros();
	}

	private async Task CommitMoveOrRestoreProject(int moveOrRestore)
	{
		List<MoveOrRestoreProject> moveOrRestoreProjects = await MoveOrRestoreProjectBatchHandler.GetCommits(moveOrRestore);
		moveOrRestoreProjects?.RemoveAll((MoveOrRestoreProject m) => UndoHelper.CanUndoIds.Contains(m.TaskId) || UndoHelper.UndoingIds.Contains(m.TaskId));
		if (moveOrRestoreProjects != null && moveOrRestoreProjects.Count > 0)
		{
			BatchUpdateResult batchUpdateResult;
			if (moveOrRestore == 2)
			{
				batchUpdateResult = await Communicator.BatchUpdateTaskProject(moveOrRestoreProjects);
				LogModel syncLog = _syncLog;
				syncLog.Log = syncLog.Log + "\r\n" + $"big sync:  move task count : {moveOrRestoreProjects.Count} , success {batchUpdateResult?.Id2etag?.Count}";
			}
			else
			{
				batchUpdateResult = await Communicator.BatchRestoreTask(moveOrRestoreProjects);
				LogModel syncLog2 = _syncLog;
				syncLog2.Log = syncLog2.Log + "\r\n" + $"big sync:  restore task count : {moveOrRestoreProjects.Count} , success {batchUpdateResult?.Id2etag?.Count}";
			}
			if (batchUpdateResult != null)
			{
				MoveOrRestoreProjectBatchHandler.HandleCommitResult(batchUpdateResult, moveOrRestore);
			}
		}
	}

	private async Task CommitTag()
	{
		BatchUpdateResult batchUpdateResult;
		do
		{
			SyncTagBean syncTagBean = await TagBatchHandler.CommitToServer();
			if (syncTagBean != null && !syncTagBean.Empty)
			{
				LogModel syncLog = _syncLog;
				syncLog.Log = syncLog.Log + "\r\n" + $" big sync: update tag count : a {syncTagBean.Add.Count} u {syncTagBean.Update.Count} d {syncTagBean.Deleted.Count}";
				batchUpdateResult = await Communicator.BatchUpdateTag(syncTagBean);
				continue;
			}
			break;
		}
		while (batchUpdateResult != null && await TagBatchHandler.HandleCommitResult(batchUpdateResult.Id2etag, batchUpdateResult.Id2error, _syncLog));
	}

	private async Task CommitClearTrash()
	{
		List<SyncStatusModel> clearTrashModels = await _taskBatchHandler.GetNeedClearTrash();
		if (!clearTrashModels.Any() || !Utils.IsNetworkAvailable() || !(await Communicator.ClearTrash()))
		{
			return;
		}
		foreach (SyncStatusModel item in clearTrashModels)
		{
			await SyncStatusDao.DeleteSyncStatus(item.EntityId, 8);
		}
		await TaskDao.ClearTrashTaskInDb();
	}

	private async Task CommitFilter()
	{
		SyncFilterBean bean = await FilterBatchHandler.CommitToServer();
		if (bean != null && !bean.Empty)
		{
			LogModel syncLog = _syncLog;
			syncLog.Log = syncLog.Log + "\r\n" + $" big sync: update filter count : a {bean.Add.Count} u {bean.Update.Count} d {bean.Deleted.Count}";
			BatchUpdateResult batchUpdateResult = await Communicator.BatchUpdateFilter(bean);
			if (batchUpdateResult != null)
			{
				await FilterBatchHandler.HandleCommitResult(batchUpdateResult, bean.Deleted, _syncLog);
			}
		}
	}

	private async Task<bool> PullMatrixProfiles(ISet<string> committedProfileIds = null)
	{
		_ = 2;
		try
		{
			if (!(await PullMatrixPreference()))
			{
				return false;
			}
			List<MatrixProfileSyncModel> list = await Communicator.GetMatrixProfiles();
			if (list != null)
			{
				_matrixProfilesPulledThisSync = true;
				LogModel syncLog = _syncLog;
				syncLog.Log = syncLog.Log + "\r\n" + $" big sync: pull matrix profiles count : {list.Count}";
				return await MatrixProfileDao.MergeFromServerAsync(list, committedProfileIds);
			}
		}
		catch (Exception ex)
		{
			LogModel syncLog2 = _syncLog;
			syncLog2.Log = syncLog2.Log + "\r\n big sync: pull matrix profiles failed " + ex.Message;
		}
		return false;
	}

	private void NotifyMatrixProfilesChanged(bool changed)
	{
		if (changed)
		{
			_syncResult.MatrixProfilesChanged = true;
			DataChangedNotifier.NotifyMatrixQuadrantChanged(0);
		}
	}

	private async Task<bool> PullMatrixPreference()
	{
		try
		{
			if (!_matrixPreferencePulledThisSync)
			{
				_matrixPreferencePulledThisSync = await SettingsHelper.PullRemotePreference(0L) != null;
			}
			return _matrixPreferencePulledThisSync;
		}
		catch (Exception ex)
		{
			LogModel syncLog = _syncLog;
			syncLog.Log = syncLog.Log + "\r\n big sync: pull matrix preference failed " + ex.Message;
			return false;
		}
	}

	private async Task CommitMatrixProfiles()
	{
		if (!_matrixProfilesPulledThisSync)
		{
			_syncLog.Log += "\r\n big sync: skip matrix commit before preference and profile pull";
			return;
		}
		ModelSyncBean<MatrixProfileSyncModel> bean = await MatrixProfileDao.BuildSyncBeanAsync();
		if (bean == null || bean.Empty())
		{
			await SettingsHelper.PushLocalPreference();
			await SettingsHelper.PushPendingMatrixShowCompleted();
			return;
		}
		LogModel syncLog = _syncLog;
		syncLog.Log = syncLog.Log + "\r\n" + $" big sync: update matrix profile count : a {bean.Add.Count} u {bean.Update.Count} d {bean.Deleted.Count}";
		BatchUpdateResult result = await Communicator.BatchUpdateMatrixProfiles(bean);
		ISet<string> committedProfileIds = GetCommittedMatrixProfileIds(result, bean);
		string defaultBeforeCommit = LocalSettings.Settings.MatrixModel?.defaultViewId;
		bool changed = await MatrixProfileDao.MarkCommittedAsync(result, bean);
		if (string.IsNullOrEmpty(defaultBeforeCommit) && !string.IsNullOrEmpty(LocalSettings.Settings.MatrixModel?.defaultViewId))
		{
			await SettingsHelper.PushLocalPreference();
		}
		_matrixPreferencePulledThisSync = false;
		changed = await PullMatrixProfiles(committedProfileIds) || changed;
		NotifyMatrixProfilesChanged(changed);
		await SettingsHelper.PushLocalPreference();
		await SettingsHelper.PushPendingMatrixShowCompleted();
	}

	private static ISet<string> GetCommittedMatrixProfileIds(BatchUpdateResult result, ModelSyncBean<MatrixProfileSyncModel> bean)
	{
		if (result == null)
		{
			return new HashSet<string>();
		}
		Dictionary<string, string> id2error = result?.Id2error ?? new Dictionary<string, string>();
		return new HashSet<string>(from id in (from p in bean.Add.Concat(bean.Update)
				select p.id).Concat(bean.Deleted)
			where !string.IsNullOrEmpty(id) && !id2error.ContainsKey(id)
			select id);
	}

	private static async Task CommitEvents()
	{
		List<SyncStatusModel> added = await EventBatchHandler.GetLocalAddedEvents();
		List<SyncStatusModel> updates = await EventBatchHandler.GetLocalModifiedEvents();
		List<SyncStatusModel> deleted = await EventBatchHandler.GetLocalDeletedEvents();
		List<SyncStatusModel> moved = await EventBatchHandler.GetLocalMoveEvents();
		if (updates.Any() || added.Any() || deleted.Any() || moved.Any())
		{
			SyncEventBean bean = await EventTransfer.DescribeSyncTaskBean(added, updates, moved, deleted);
			SyncEventBean calDavBean = await EventTransfer.DescribeSyncTaskBean(added, updates, moved, deleted, isCalDav: true);
			if (bean.CalendarAccountBeans.Any())
			{
				await EventBatchHandler.HandleCommitResult(await Communicator.BatchUpdateCalendarEvents(bean), bean);
			}
			if (calDavBean.CalendarAccountBeans.Any())
			{
				await EventBatchHandler.HandleCommitResult(await Communicator.BatchUpdateCalDavEvents(calDavBean), calDavBean, checkEtag: false);
			}
		}
	}

	private async Task CommitTask()
	{
		BlockingSet<string> undoTaskIds = new BlockingSet<string>();
		undoTaskIds.AddRange(UndoHelper.CanUndoIds);
		undoTaskIds.AddRange(UndoHelper.UndoingIds);
		undoTaskIds.AddRange(CloseUndoHandler.GetAffectedTaskIds());
		List<SyncStatusModel> createdTasks = (await _taskBatchHandler.GetNeedPostCreatedTasks()).Where((SyncStatusModel s) => !undoTaskIds.Contains(s.EntityId)).ToList();
		List<SyncStatusModel> updatedTasks = (await _taskBatchHandler.GetLocalContentUpdatedTasks()).Where((SyncStatusModel s) => !undoTaskIds.Contains(s.EntityId)).ToList();
		List<SyncStatusModel> deletedTasks = await _taskBatchHandler.GetNeedPostDeletedTasks();
		List<SyncStatusModel> list = await _taskBatchHandler.GetNeedPostDeleteForeverTasks();
		if (createdTasks.Count <= 0 && updatedTasks.Count <= 0 && deletedTasks.Count <= 0 && list.Count <= 0)
		{
			return;
		}
		ReminderCalculator.AssembleReminders();
		_syncLog.Log += $"\r\n big sync: update task add:{createdTasks.Count} updated:{updatedTasks.Count} ";
		if (deletedTasks.Count > 0)
		{
			string text = deletedTasks.Select((SyncStatusModel t) => t.EntityId).Join(";");
			LogModel syncLog = _syncLog;
			syncLog.Log = syncLog.Log + "\r\n deleteIds : " + text;
		}
		foreach (SyncTaskBean taskBean in await TaskTransfer.DescribeSyncTaskBean(createdTasks, updatedTasks, deletedTasks, list))
		{
			if (taskBean.DeletedFromTrash.Count > 0)
			{
				string text2 = taskBean.DeletedFromTrash.Select((TaskProject t) => t.taskId).Join(";");
				LogModel syncLog2 = _syncLog;
				syncLog2.Log = syncLog2.Log + "\r\n deleteForeverIds : " + text2;
				await TaskBatchHandler.HandleCommitResult(taskBean, await Communicator.BatchDeleteTask(taskBean.DeletedFromTrash), _syncLog);
			}
			if (taskBean.Empty)
			{
				continue;
			}
			if (taskBean.Delete.Count > 0)
			{
				string text3 = taskBean.Delete.Select((TaskProject t) => (t.taskId.Length <= 6) ? t.taskId : t.taskId.Substring(t.taskId.Length - 6, 6)).Join(";");
				LogModel syncLog3 = _syncLog;
				syncLog3.Log = syncLog3.Log + "\r\n deleteTaskIds : " + text3;
			}
			await TaskBatchHandler.HandleCommitResult(taskBean, await Communicator.BatchUpdateTask(taskBean), _syncLog);
		}
	}

	private async Task CommitMoveColumnProject(bool checkReCommit)
	{
		List<MoveColumnArgs> columnMoveStatus = await MoveColumnProjectBatchHandler.GetCommits();
		if (columnMoveStatus != null && columnMoveStatus.Count > 0)
		{
			BatchUpdateResult batchUpdateResult = await Communicator.MoveColumnAsync(columnMoveStatus);
			LogModel syncLog = _syncLog;
			syncLog.Log = syncLog.Log + "\r\n" + $"big sync:  restore task count : {columnMoveStatus.Count} , success {batchUpdateResult?.Id2etag?.Count}";
			if (batchUpdateResult != null && await MoveColumnProjectBatchHandler.HandleCommitResult(batchUpdateResult, columnMoveStatus) && checkReCommit)
			{
				await CommitMoveColumnProject(checkReCommit: false);
			}
		}
	}

	public void Clear()
	{
		_projectGroupBatchHandler = null;
		_projectBatchHandler = null;
		_taskBatchHandler = null;
		_sortOrderBatchHandler = null;
		_syncOrderBatchHandler = null;
		_filterBatchHandler = null;
		_tagBatchHandler = null;
		_eventBatchHandler = null;
	}
}
