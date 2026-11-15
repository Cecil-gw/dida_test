using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.SyncServices.Models;
using ticktick_WPF.Util;
using ticktick_WPF.Util.Sync;
using ticktick_WPF.Views.Tag;

namespace ticktick_WPF.SyncServices;

public static class PomoSyncService
{
	private static readonly object PullRemoteLock = new object();

	private static bool _isPullingRemote;

	private static bool _hasPendingRemotePull;

	private static DateTime _pendingRemotePullStart;

	private static DateTime _pendingRemotePullEnd;

	private static readonly SemaphoreSlim CardPullLock = new SemaphoreSlim(1, 1);

	private const int PullRemoteThrottleSeconds = 60;

	private static readonly Dictionary<(DateTime, DateTime), DateTime> RecentPullTimes = new Dictionary<(DateTime, DateTime), DateTime>();

	private static StatisticsModel _postCommitExpectedStatistics;

	private static StatisticsModel _postCommitDelta;

	private static long _timelinePullPoint;

	private static long _timerPullPoint;

	private static string _timerPullId;

	private static readonly SemaphoreSlim TimerPullLock = new SemaphoreSlim(1, 1);

	public static async Task PullForCardAsync(string focusId, int? focusType)
	{
		if (string.IsNullOrWhiteSpace(focusId))
		{
			return;
		}
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		await CardPullLock.WaitAsync();
		try
		{
			PomodoroModel pomodoroModel = await PomoDao.GetPomoById(focusId);
			int? type = ((focusType == 0 || focusType == 1) ? focusType : pomodoroModel?.Type);
			PomodoroModel remote = await Communicator.PullRemoteFocus(focusId, type != 1);
			if (remote == null && !type.HasValue)
			{
				type = 1;
				remote = await Communicator.PullRemoteFocus(focusId, isPomo: false);
			}
			if (remote != null && string.Equals(remote.Id, focusId, StringComparison.Ordinal))
			{
				pomodoroModel = await PomoDao.GetPomoById(focusId);
				if (isCurrentUser())
				{
					await MergeRemoteRecords(new List<PomodoroModel> { remote }, (pomodoroModel == null) ? new List<PomodoroModel>() : new List<PomodoroModel> { pomodoroModel }, remote.StartTime, remote.EndTime, type.GetValueOrDefault(), isCompleteRemoteResult: false);
				}
			}
		}
		finally
		{
			CardPullLock.Release();
		}
	}

	public static Task TryPullRemote(DateTime startTime, DateTime endTime)
	{
		lock (PullRemoteLock)
		{
			if (_isPullingRemote)
			{
				_pendingRemotePullStart = startTime;
				_pendingRemotePullEnd = endTime;
				_hasPendingRemotePull = true;
				return Task.CompletedTask;
			}
			_isPullingRemote = true;
		}
		return PullRemoteWithLatestPendingRequest(startTime, endTime);
	}

	private static async Task PullRemoteWithLatestPendingRequest(DateTime startTime, DateTime endTime)
	{
		while (true)
		{
			try
			{
				await PullRemote(startTime, endTime);
			}
			catch (Exception arg)
			{
				UtilLog.Error(string.Format("{0}.PullRemote failed: {1}", "PomoSyncService", arg));
			}
			lock (PullRemoteLock)
			{
				if (!_hasPendingRemotePull)
				{
					_isPullingRemote = false;
					break;
				}
				startTime = _pendingRemotePullStart;
				endTime = _pendingRemotePullEnd;
				_hasPendingRemotePull = false;
			}
		}
	}

	private static async Task<bool> PullRemote(DateTime startTime, DateTime endTime)
	{
		if (startTime > endTime)
		{
			return false;
		}
		DateTime now = DateTime.Now;
		foreach (var item in (from pair in RecentPullTimes
			where (now - pair.Value).TotalSeconds >= 60.0
			select pair.Key).ToList())
		{
			RecentPullTimes.Remove(item);
		}
		(DateTime, DateTime) key = (startTime, endTime);
		if (RecentPullTimes.ContainsKey(key))
		{
			return false;
		}
		RecentPullTimes[key] = now;
		DateTime dateTime = LocalSettings.Settings.PomoLocalSetting.EarliestFetchDate;
		DateTime origin = dateTime;
		DateTime dateTime2 = DateTime.Today.AddDays(1.0);
		if (Utils.IsEmptyDate(dateTime))
		{
			dateTime = DateTime.Today.AddDays(1.0);
		}
		if (startTime < dateTime)
		{
			if ((dateTime - startTime).Days > 100)
			{
				await Pull(startTime, endTime, isPomo: false);
				await Pull(startTime, endTime, isPomo: true);
				return true;
			}
			if (dateTime.AddDays(-31.0) < startTime)
			{
				startTime = dateTime.AddDays(-31.0);
			}
			await Pull(startTime, endTime, isPomo: true);
			await Pull(startTime, endTime, isPomo: false);
			dateTime = startTime;
		}
		else
		{
			if ((dateTime2 - endTime).Days > 14)
			{
				return false;
			}
			if (startTime > dateTime2)
			{
				return false;
			}
			if ((dateTime2 - endTime).Days < 14)
			{
				endTime = endTime.AddDays(14 - (endTime - dateTime2).Days);
			}
			if (dateTime2 < endTime)
			{
				endTime = dateTime2;
			}
			await Pull(startTime, endTime, isPomo: false);
			await Pull(startTime, endTime, isPomo: true);
			dateTime = startTime;
		}
		UtilLog.Info("PomoSyncService.PullRemote, Param: " + Utils.GetSafeDateLogText(startTime) + "," + Utils.GetSafeDateLogText(endTime) + ",Set EarliestFetchDate: " + Utils.GetSafeDateLogText(origin) + " to " + Utils.GetSafeDateLogText(dateTime));
		LocalSettings.Settings.PomoLocalSetting.EarliestFetchDate = dateTime;
		return true;
	}

	public static async Task<bool> Pull7Days(bool isPomo)
	{
		return await Pull(DateTime.Today.AddDays(-7.0), DateTime.Today.AddDays(1.0), isPomo);
	}

	public static Task<bool> PullTimerHistory(DateTime start, DateTime end, CancellationToken cancellationToken)
	{
		return PullTimerHistoryCore(start, end, DateTime.Today.AddDays(1.0), (DateTime from, DateTime to, bool isPomo) => (!isPomo) ? Communicator.PullRemoteTimings(from, to) : Communicator.PullRemotePomos(from, to), MergeFilteredRemotePage, cancellationToken);
	}

	internal static async Task<bool> PullTimerHistoryCore(DateTime start, DateTime end, DateTime todayEnd, Func<DateTime, DateTime, bool, Task<List<PomodoroModel>>> pull, Func<List<PomodoroModel>, DateTime, DateTime, int?, Task<bool>> merge, CancellationToken cancellationToken)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		await TimerPullLock.WaitAsync(cancellationToken);
		try
		{
			end = ((end > todayEnd) ? todayEnd : end);
			bool changed = false;
			HashSet<(int, string)> mergedIds = new HashSet<(int, string)>();
			while (start < end)
			{
				DateTime segmentEnd = (((end - start).TotalDays > 29.0) ? start.AddDays(29.0) : end);
				DateTime pullStart = ((start > DateTime.MinValue.AddDays(1.0)) ? start.AddDays(-1.0) : DateTime.MinValue);
				DateTime pullEnd = (((todayEnd - segmentEnd).TotalDays > 1.0) ? segmentEnd.AddDays(1.0) : todayEnd);
				int type;
				for (type = 0; type < 2; type++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					List<PomodoroModel> remotes = await pull(pullStart, pullEnd, type == 0);
					cancellationToken.ThrowIfCancellationRequested();
					if (!isCurrentUser())
					{
						return false;
					}
					if (remotes == null)
					{
						continue;
					}
					remotes = (from p in remotes
						where string.IsNullOrEmpty(p.Id) || !mergedIds.Contains((type, p.Id))
						group p by p.Id into @group
						select @group.First()).ToList();
					if (remotes.Count <= 0)
					{
						continue;
					}
					changed = await merge(remotes, pullStart, pullEnd, type) || changed;
					foreach (PomodoroModel item in remotes)
					{
						if (!string.IsNullOrEmpty(item.Id))
						{
							mergedIds.Add((type, item.Id));
						}
					}
				}
				start = segmentEnd;
			}
			return changed;
		}
		finally
		{
			TimerPullLock.Release();
		}
	}

	public static async Task<bool> Pull(DateTime start, DateTime end, bool isPomo)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		List<PomodoroModel> list = ((!isPomo) ? (await Communicator.PullRemoteTimings(start, end)) : (await Communicator.PullRemotePomos(start, end)));
		List<PomodoroModel> list2 = list;
		if (list2 == null || !isCurrentUser())
		{
			return false;
		}
		return await MergeRemotes(list2, start, end, (!isPomo) ? 1 : 0);
	}

	private static async Task<bool> MergeRemotePomoOfTimer(List<PomodoroModel> remotes, DateTime start, DateTime end, string timerId)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		TimerModel timerModel = await TimerDao.GetTimerById(timerId);
		if (timerModel == null)
		{
			return false;
		}
		List<PomodoroModel> locals = await PomoDao.GetPomoDescByStartAndTimerId(0, timerId, timerModel.ObjId, timerModel.ObjType == "habit", start, end);
		if (!isCurrentUser())
		{
			return false;
		}
		return await MergeTimerRecords(remotes, locals, start, end, timerId);
	}

	internal static async Task<bool> MergeTimerRecords(List<PomodoroModel> remotes, List<PomodoroModel> locals, DateTime start, DateTime end, string timerId)
	{
		foreach (PomodoroModel remote in remotes)
		{
			LocalSettings.Settings.ObserveSyncedFocusRecord(remote);
		}
		int updated = 0;
		int added = 0;
		Dictionary<string, PomodoroModel> localDict = new Dictionary<string, PomodoroModel>();
		foreach (PomodoroModel local2 in locals)
		{
			if (!string.IsNullOrEmpty(local2.Id))
			{
				localDict[local2.Id] = local2;
			}
		}
		if (remotes.Count > 0)
		{
			foreach (PomodoroModel pomo in remotes)
			{
				PomodoroModel replacedPomo = null;
				List<PomoTask> replacedPomoTasks = null;
				if (localDict.ContainsKey(pomo.Id))
				{
					PomodoroModel local = localDict[pomo.Id];
					locals.Remove(local);
					local = (await PomoDao.GetPomoById(local.Id)) ?? local;
					if (local.SyncStatus != 2)
					{
						continue;
					}
					pomo._Id = local._Id;
					pomo.SyncStatus = 2;
					pomo.UserId = local.UserId;
					pomo.Type = local.Type;
					if (!(await IsRemotePomoChanged(local, pomo)))
					{
						string id = local.Id;
						int type = local.Type;
						bool added2 = local.Added;
						await ReconcileHistoricalRepeatPomoSummary(id, type, added2, await PomoService.GetAdjustPomoTasks(local.Id, local));
						continue;
					}
					replacedPomo = local;
					replacedPomoTasks = await PomoService.GetAdjustPomoTasks(local.Id, local);
					await PomoDao.UpdateAsync(pomo);
					await PomoTaskDao.DeleteAllByPomoId(local.Id);
					updated++;
				}
				else
				{
					pomo.SyncStatus = 2;
					pomo.UserId = Utils.GetCurrentUserStr();
					await PomoDao.InsertAsync(pomo);
					added++;
				}
				if (pomo.Tasks == null || pomo.Tasks.Length == 0)
				{
					if (replacedPomoTasks != null)
					{
						await UpdatePulledPomoSummary(replacedPomo.Id, replacedPomo.Type, replacedPomoTasks, -1);
					}
					continue;
				}
				PomoTask[] tasks = pomo.Tasks;
				foreach (PomoTask pomoTask in tasks)
				{
					pomoTask.PomoId = pomo.Id;
					if (pomoTask.Tags != null)
					{
						pomoTask.TagString = TagSerializer.ToJsonContent(pomoTask.Tags.ToList());
					}
				}
				bool pomoTasksInserted = false;
				try
				{
					await PomoTaskDao.InsertAllAsync(pomo.Tasks);
					pomoTasksInserted = true;
				}
				catch (Exception ex)
				{
					UtilLog.Warn("PullTimerTasks" + ex.Message);
				}
				if (pomoTasksInserted)
				{
					if (replacedPomoTasks != null)
					{
						await UpdatePulledPomoSummary(replacedPomo.Id, replacedPomo.Type, replacedPomoTasks, -1);
					}
					string id = pomo.Id;
					int type = pomo.Type;
					await UpdatePulledPomoSummary(id, type, await PomoService.GetAdjustPomoTasks(pomo.Id, pomo), 1, pomo.Added);
				}
			}
		}
		bool flag = added > 0 || updated > 0;
		if (flag)
		{
			TimerSyncService.ClearPulledKey();
			PomoNotifier.NotifyServiceChanged();
			UtilLog.Info($"PomoSyncService.PullTimerFocus, Param: {Utils.GetSafeDateLogText(start)},{Utils.GetSafeDateLogText(end)},{timerId}  Remote:{remotes.Count} Locals:{locals.Count} CURD: ({added}, {updated}, 0, 0)");
		}
		return flag;
	}

	public static Task<bool> MergeRemotes(List<PomodoroModel> remotes, DateTime start, DateTime end, int? type)
	{
		return MergeRemotesCore(remotes, start, end, type, isCompleteRemoteResult: true);
	}

	internal static Task<bool> MergeFilteredRemotePage(List<PomodoroModel> remotes, DateTime start, DateTime end, int? type)
	{
		return MergeRemotesCore(remotes, start, end, type, isCompleteRemoteResult: false);
	}

	private static async Task<bool> MergeRemotesCore(List<PomodoroModel> remotes, DateTime start, DateTime end, int? type, bool isCompleteRemoteResult)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		List<PomodoroModel> list = await PomoDao.GetPomoByDateSpan(start, end, containDeleted: true);
		if (!isCurrentUser())
		{
			return false;
		}
		list = ((!type.HasValue) ? list : list.Where((PomodoroModel p) => p.Type == type.Value).ToList());
		return await MergeRemoteRecords(remotes, list, start, end, type, isCompleteRemoteResult);
	}

	private static async Task<bool> MergeRemoteRecords(List<PomodoroModel> remotes, List<PomodoroModel> locals, DateTime start, DateTime end, int? type, bool isCompleteRemoteResult)
	{
		foreach (PomodoroModel remote in remotes)
		{
			LocalSettings.Settings.ObserveSyncedFocusRecord(remote);
		}
		string deleted = "";
		int updated = 0;
		string added = "";
		Dictionary<string, PomodoroModel> localDict = new Dictionary<string, PomodoroModel>();
		foreach (PomodoroModel local2 in locals)
		{
			if (!string.IsNullOrEmpty(local2.Id))
			{
				localDict[local2.Id] = local2;
			}
		}
		if (remotes.Count > 0)
		{
			foreach (PomodoroModel pomo in remotes)
			{
				PomodoroModel replacedPomo = null;
				List<PomoTask> replacedPomoTasks = null;
				if (localDict.ContainsKey(pomo.Id))
				{
					PomodoroModel local = localDict[pomo.Id];
					locals.Remove(local);
					if (local.SyncStatus != 2 && local.SyncStatus != -2 && local.SyncStatus != 0)
					{
						local.Note = MergeUtils.GetRevisedDiffText(string.Empty, pomo.Note, local.Note);
						await PomoDao.UpdateAsync(local);
						continue;
					}
					bool num = local.SyncStatus == -2 || local.SyncStatus == 0;
					pomo._Id = local._Id;
					pomo.SyncStatus = 2;
					pomo.UserId = local.UserId;
					pomo.Type = local.Type;
					if (num)
					{
						pomo.Note = MergeUtils.GetRevisedDiffText(string.Empty, pomo.Note, local.Note);
						pomo.SyncStatus = 1;
					}
					bool flag = !num;
					if (flag)
					{
						flag = !(await IsRemotePomoChanged(local, pomo));
					}
					if (flag)
					{
						string id = local.Id;
						int type2 = local.Type;
						bool added2 = local.Added;
						await ReconcileHistoricalRepeatPomoSummary(id, type2, added2, await PomoService.GetAdjustPomoTasks(local.Id, local));
						continue;
					}
					replacedPomo = local;
					replacedPomoTasks = await PomoService.GetAdjustPomoTasks(local.Id, local);
					await PomoDao.UpdateAsync(pomo);
					if (await PomoTaskDao.DeleteAllByPomoId(local.Id) != pomo.Tasks?.Length)
					{
						UtilLog.Warn("MergePomo: " + pomo.Id + ",RemoteTasks: " + pomo.Tasks?.Length);
					}
					updated++;
				}
				else
				{
					pomo.SyncStatus = 2;
					pomo.UserId = Utils.GetCurrentUserStr();
					pomo.Type = type ?? pomo.Type;
					await PomoDao.InsertAsync(pomo);
					added += pomo.Id;
				}
				if (pomo.Tasks == null || pomo.Tasks.Length == 0)
				{
					if (replacedPomoTasks != null)
					{
						await UpdatePulledPomoSummary(replacedPomo.Id, replacedPomo.Type, replacedPomoTasks, -1);
					}
					continue;
				}
				PomoTask[] tasks = pomo.Tasks;
				foreach (PomoTask pomoTask in tasks)
				{
					pomoTask.PomoId = pomo.Id;
					if (pomoTask.Tags != null)
					{
						pomoTask.TagString = TagSerializer.ToJsonContent(pomoTask.Tags.ToList());
					}
				}
				bool pomoTasksInserted = false;
				try
				{
					await PomoTaskDao.InsertAllAsync(pomo.Tasks);
					pomoTasksInserted = true;
				}
				catch (Exception ex)
				{
					UtilLog.Warn("PullPomoTasks" + ex.Message);
				}
				if (pomoTasksInserted)
				{
					if (replacedPomoTasks != null)
					{
						await UpdatePulledPomoSummary(replacedPomo.Id, replacedPomo.Type, replacedPomoTasks, -1);
					}
					string id = pomo.Id;
					int type2 = pomo.Type;
					await UpdatePulledPomoSummary(id, type2, await PomoService.GetAdjustPomoTasks(pomo.Id, pomo), 1, pomo.Added);
				}
			}
		}
		foreach (PomodoroModel pomo in GetRemoteDeletionCandidates(locals, end, isCompleteRemoteResult))
		{
			List<PomoTask> pomoTasks = await PomoService.GetAdjustPomoTasks(pomo.Id, pomo);
			await UpdatePulledPomoSummary(pomo.Id, pomo.Type, pomoTasks, -1);
			await PomoDao.DeleteAsync(pomo);
			await PomoTaskDao.DeleteAllByPomoId(pomo.Id);
			deleted += pomo.Id;
		}
		bool flag2 = !string.IsNullOrEmpty(added) || updated > 0 || !string.IsNullOrEmpty(deleted);
		if (flag2)
		{
			TimerSyncService.ClearPulledKey();
			PomoNotifier.NotifyServiceChanged();
			UtilLog.Info($"PomoSyncService.Pull, Param: {Utils.GetSafeDateLogText(start)},{Utils.GetSafeDateLogText(end)},{type}  Remote:{remotes.Count} Locals:{locals.Count} CURD: ({added} | {updated} | {deleted})");
		}
		return flag2;
	}

	internal static List<PomodoroModel> GetRemoteDeletionCandidates(IEnumerable<PomodoroModel> unmatchedLocals, DateTime end, bool isCompleteRemoteResult)
	{
		if (!isCompleteRemoteResult || unmatchedLocals == null)
		{
			return new List<PomodoroModel>();
		}
		return unmatchedLocals.Where((PomodoroModel local) => local != null && local.SyncStatus == 2 && local.EndTime <= end).ToList();
	}

	private static async Task UpdatePulledPomoSummary(string pomoId, int pomoType, IEnumerable<PomoTask> pomoTasks, int rate, bool isManuallyAdded = false)
	{
		if (string.IsNullOrEmpty(pomoId))
		{
			return;
		}
		List<string> changedTaskIds = new List<string>();
		foreach (IGrouping<string, PomoTask> group in from task in pomoTasks ?? Enumerable.Empty<PomoTask>()
			where task != null && !string.IsNullOrEmpty(task.TaskId)
			group task by task.TaskId)
		{
			long duration = group.Sum((PomoTask task) => Utils.GetTotalSecond(task.StartTime, task.EndTime));
			TaskModel taskModel = await TaskDao.GetThinTaskById(group.Key);
			if (duration <= 0 || taskModel == null)
			{
				continue;
			}
			if (rate > 0 && !isManuallyAdded && ShouldSkipHistoricalRepeatPomoSummary(taskModel, group))
			{
				PomodoroSummaryModel pomodoroSummaryModel = await PomoSummaryDao.GetPomoByTaskId(group.Key);
				if (RemovePulledPomoSummaryFocus(pomodoroSummaryModel, pomoId, pomoType, duration, allowAggregateFallback: false))
				{
					await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel);
					changedTaskIds.Add(group.Key);
				}
				continue;
			}
			PomodoroSummaryModel pomodoroSummaryModel2 = await PomoSummaryDao.GetPomoByTaskId(group.Key);
			bool flag = false;
			if (rate > 0)
			{
				if (pomodoroSummaryModel2 == null)
				{
					pomodoroSummaryModel2 = new PomodoroSummaryModel
					{
						id = Utils.GetGuid(),
						userId = LocalSettings.Settings.LoginUserId,
						estimatedPomo = 0,
						EstimatedDuration = 0L,
						taskId = group.Key
					};
					flag = true;
				}
				object[] array = pomodoroSummaryModel2.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoId);
				if (array != null)
				{
					if (!IsFocusDurationEqual(array[2], duration))
					{
						array[2] = duration;
						flag = true;
					}
				}
				else
				{
					pomodoroSummaryModel2.AddFocuses(new object[3]
					{
						pomoId,
						(pomoType != 0) ? 1 : 0,
						duration
					});
					flag = true;
				}
			}
			else
			{
				if (pomodoroSummaryModel2 == null)
				{
					continue;
				}
				flag = RemovePulledPomoSummaryFocus(pomodoroSummaryModel2, pomoId, pomoType, duration, allowAggregateFallback: true);
			}
			if (flag)
			{
				await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel2);
				changedTaskIds.Add(group.Key);
			}
		}
		if (changedTaskIds.Count > 0)
		{
			TaskChangeNotifier.NotifyTaskPomoAdded(changedTaskIds.Distinct().ToList());
		}
	}

	private static async Task ReconcileHistoricalRepeatPomoSummary(string pomoId, int pomoType, bool isManuallyAdded, IEnumerable<PomoTask> pomoTasks)
	{
		if (string.IsNullOrEmpty(pomoId) || isManuallyAdded)
		{
			return;
		}
		List<string> changedTaskIds = new List<string>();
		foreach (IGrouping<string, PomoTask> group in from task in pomoTasks ?? Enumerable.Empty<PomoTask>()
			where task != null && !string.IsNullOrEmpty(task.TaskId)
			group task by task.TaskId)
		{
			long duration = group.Sum((PomoTask task) => Utils.GetTotalSecond(task.StartTime, task.EndTime));
			TaskModel taskModel = await TaskDao.GetThinTaskById(group.Key);
			if (duration > 0 && taskModel != null && ShouldSkipHistoricalRepeatPomoSummary(taskModel, group))
			{
				PomodoroSummaryModel pomodoroSummaryModel = await PomoSummaryDao.GetPomoByTaskId(group.Key);
				if (RemovePulledPomoSummaryFocus(pomodoroSummaryModel, pomoId, pomoType, duration, allowAggregateFallback: false))
				{
					await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel);
					changedTaskIds.Add(group.Key);
				}
			}
		}
		if (changedTaskIds.Count > 0)
		{
			TaskChangeNotifier.NotifyTaskPomoAdded(changedTaskIds.Distinct().ToList());
		}
	}

	private static bool IsFocusDurationEqual(object value, long duration)
	{
		try
		{
			return Convert.ToInt64(value) == duration;
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static bool RemovePulledPomoSummaryFocus(PomodoroSummaryModel summary, string pomoId, int pomoType, long duration, bool allowAggregateFallback)
	{
		if (summary == null)
		{
			return false;
		}
		object[] array = summary.focuses?.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoId);
		if (array != null)
		{
			summary.focuses.Remove(array);
			summary.CheckNegative();
			return true;
		}
		if (!allowAggregateFallback)
		{
			return false;
		}
		if (pomoType == 0)
		{
			summary.count -= ((duration >= Constants.MinFocusDuration) ? 1 : 0);
			summary.PomoDuration -= duration;
		}
		else
		{
			summary.StopwatchDuration -= duration;
		}
		summary.CheckNegative();
		return true;
	}

	private static bool ShouldSkipHistoricalRepeatPomoSummary(TaskModel task, IEnumerable<PomoTask> pomoTasks)
	{
		if (task.status != 0 || string.IsNullOrEmpty(task.repeatFlag) || Utils.IsEmptyDate(task.startDate))
		{
			return false;
		}
		DateTime date = (from pomoTask in pomoTasks
			where pomoTask != null && !Utils.IsEmptyDate(pomoTask.StartTime)
			select pomoTask.StartTime).DefaultIfEmpty().Min();
		if (Utils.IsEmptyDate(date))
		{
			return false;
		}
		return date.Date < task.startDate.Value.Date;
	}

	private static async Task<bool> IsRemotePomoChanged(PomodoroModel local, PomodoroModel remote)
	{
		if (local == null || remote == null)
		{
			return local != remote;
		}
		if (local.Status != remote.Status || local.StartTime != remote.StartTime || local.EndTime != remote.EndTime || local.AdjustTime != remote.AdjustTime || local.Type != remote.Type || local.PauseDuration != remote.PauseDuration || local.Added != remote.Added || !StringEquals(local.TaskSid, remote.TaskSid) || !StringEquals(local.Note, remote.Note))
		{
			return true;
		}
		return ArePomoTasksChanged(await PomoDao.GetPomoTasksByPomoId(local.Id), remote.Tasks);
	}

	private static bool ArePomoTasksChanged(IEnumerable<PomoTask> locals, IEnumerable<PomoTask> remotes)
	{
		List<PomoTask> list = OrderPomoTasks(locals).ToList();
		List<PomoTask> list2 = OrderPomoTasks(remotes).ToList();
		if (list.Count != list2.Count)
		{
			return true;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (IsPomoTaskChanged(list[i], list2[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static IEnumerable<PomoTask> OrderPomoTasks(IEnumerable<PomoTask> tasks)
	{
		return from t in tasks ?? Enumerable.Empty<PomoTask>()
			orderby t.StartTime, t.EndTime, t.TaskId, t.HabitId, t.TimerSid
			select t;
	}

	private static bool IsPomoTaskChanged(PomoTask local, PomoTask remote)
	{
		if (local != null && remote != null && !(local.StartTime != remote.StartTime) && !(local.EndTime != remote.EndTime) && StringEquals(local.TaskId, remote.TaskId) && StringEquals(local.HabitId, remote.HabitId) && StringEquals(local.Title, remote.Title) && StringEquals(local.ProjectName, remote.ProjectName) && StringEquals(local.TimerSid, remote.TimerSid) && StringEquals(local.TimerName, remote.TimerName))
		{
			return !StringEquals(GetPomoTaskTagString(local), GetPomoTaskTagString(remote));
		}
		return true;
	}

	private static string GetPomoTaskTagString(PomoTask task)
	{
		if (task?.Tags != null)
		{
			return TagSerializer.ToJsonContent(task.Tags.ToList());
		}
		return task?.TagString;
	}

	private static bool StringEquals(string left, string right)
	{
		return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
	}

	public static async Task PullStatistics()
	{
		StatisticsModel statistics = await Communicator.GetStatistics();
		if (statistics != null)
		{
			statistics.Add(await GetPendingStatisticsDelta());
			ApplyPostCommitStatisticsGuard(statistics);
			statistics.Date = DateUtils.GetDateNum(DateTime.Today);
			LocalSettings.Settings.StatisticsModel = statistics;
		}
		else if (LocalSettings.Settings.StatisticsModel == null)
		{
			List<PomodoroModel> pomoModels = await PomoDao.GetAllPomo();
			for (int i = 0; i < pomoModels.Count; i++)
			{
				List<PomodoroModel> list = pomoModels;
				int index = i;
				list[index] = await PomoService.GetAdjustPomo(pomoModels[i]);
			}
			LocalSettings.Settings.StatisticsModel = PomoService.GetStatisticsModel(pomoModels, checkDelete: false);
			LocalSettings.Settings.StatisticsModel.Date = DateUtils.GetDateNum(DateTime.Today);
		}
		else if (LocalSettings.Settings.StatisticsModel.Date < DateUtils.GetDateNum(DateTime.Today))
		{
			List<PomodoroModel> pomoModels = await PomoDao.GetNeedPostPomos();
			for (int i = 0; i < pomoModels.Count; i++)
			{
				List<PomodoroModel> list = pomoModels;
				int index = i;
				list[index] = await PomoService.GetAdjustPomo(pomoModels[i]);
			}
			StatisticsModel statisticsModel = PomoService.GetStatisticsModel(pomoModels, checkDelete: true);
			LocalSettings.Settings.StatisticsModel.todayPomoCount = statisticsModel.todayPomoCount;
			LocalSettings.Settings.StatisticsModel.todayPomoDuration = statisticsModel.todayPomoDuration;
			LocalSettings.Settings.StatisticsModel.Date = DateUtils.GetDateNum(DateTime.Today);
		}
	}

	private static async Task<StatisticsModel> GetPendingStatisticsDelta()
	{
		List<PomodoroModel> pomoModels = (await PomoDao.GetNeedPostPomos())?.Where((PomodoroModel p) => p.SyncStatus == 0 || p.SyncStatus == -1).ToList() ?? new List<PomodoroModel>();
		for (int i = 0; i < pomoModels.Count; i++)
		{
			List<PomodoroModel> list = pomoModels;
			int index = i;
			list[index] = await PomoService.GetAdjustPomo(pomoModels[i]);
		}
		return PomoService.GetStatisticsModel(pomoModels, checkDelete: true);
	}

	private static void ApplyPostCommitStatisticsGuard(StatisticsModel statistics)
	{
		if (statistics != null && _postCommitExpectedStatistics != null && _postCommitDelta != null)
		{
			statistics.todayPomoCount = ApplyPostCommitFieldGuard(statistics.todayPomoCount, _postCommitExpectedStatistics.todayPomoCount, _postCommitDelta.todayPomoCount);
			statistics.totalPomoCount = ApplyPostCommitFieldGuard(statistics.totalPomoCount, _postCommitExpectedStatistics.totalPomoCount, _postCommitDelta.totalPomoCount);
			statistics.todayPomoDuration = ApplyPostCommitFieldGuard(statistics.todayPomoDuration, _postCommitExpectedStatistics.todayPomoDuration, _postCommitDelta.todayPomoDuration);
			statistics.totalPomoDuration = ApplyPostCommitFieldGuard(statistics.totalPomoDuration, _postCommitExpectedStatistics.totalPomoDuration, _postCommitDelta.totalPomoDuration);
			_postCommitExpectedStatistics = null;
			_postCommitDelta = null;
		}
	}

	private static int ApplyPostCommitFieldGuard(int remoteValue, int expectedValue, int delta)
	{
		if (delta > 0)
		{
			return Math.Max(remoteValue, expectedValue);
		}
		if (delta < 0)
		{
			return Math.Min(remoteValue, expectedValue);
		}
		return remoteValue;
	}

	private static long ApplyPostCommitFieldGuard(long remoteValue, long expectedValue, long delta)
	{
		if (delta > 0)
		{
			return Math.Max(remoteValue, expectedValue);
		}
		if (delta < 0)
		{
			return Math.Min(remoteValue, expectedValue);
		}
		return remoteValue;
	}

	private static StatisticsModel CloneStatistics(StatisticsModel statistics)
	{
		if (statistics == null)
		{
			return null;
		}
		return new StatisticsModel
		{
			todayPomoCount = statistics.todayPomoCount,
			totalPomoCount = statistics.totalPomoCount,
			todayPomoDuration = statistics.todayPomoDuration,
			totalPomoDuration = statistics.totalPomoDuration,
			Date = statistics.Date
		};
	}

	private static async Task PreparePostCommitStatisticsGuard(List<PomodoroModel> needPostPomos)
	{
		_postCommitExpectedStatistics = null;
		_postCommitDelta = null;
		if (LocalSettings.Settings.StatisticsModel == null || needPostPomos == null)
		{
			return;
		}
		List<PomodoroModel> statisticsPomos = needPostPomos.Where((PomodoroModel p) => p.SyncStatus == 0 || p.SyncStatus == -1).ToList();
		if (statisticsPomos.Any())
		{
			for (int i = 0; i < statisticsPomos.Count; i++)
			{
				List<PomodoroModel> list = statisticsPomos;
				int index = i;
				list[index] = await PomoService.GetAdjustPomo(statisticsPomos[i]);
			}
			StatisticsModel statisticsModel = PomoService.GetStatisticsModel(statisticsPomos, checkDelete: true);
			if (statisticsModel.todayPomoCount != 0 || statisticsModel.totalPomoCount != 0 || statisticsModel.todayPomoDuration != 0L || statisticsModel.totalPomoDuration != 0L)
			{
				_postCommitExpectedStatistics = CloneStatistics(LocalSettings.Settings.StatisticsModel);
				_postCommitDelta = statisticsModel;
			}
		}
	}

	public static async Task<bool> PullFocusTimelines(bool resetTime = false, string timerId = null)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		_timelinePullPoint = ((resetTime || _timelinePullPoint == 0L) ? Utils.GetNowTimeStampInMills() : _timelinePullPoint);
		long point = _timelinePullPoint;
		if (point < 0)
		{
			return false;
		}
		List<PomodoroModel> list = await Communicator.GetFocusTimeline(point);
		if (list != null && isCurrentUser())
		{
			DateTime end = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).AddMilliseconds(point);
			list = list.OrderByDescending((PomodoroModel f) => f.StartTime).ToList();
			DateTime dateTime = list.LastOrDefault()?.StartTime ?? default(DateTime);
			_timelinePullPoint = ((list.Count >= 30) ? Utils.GetTimeStamp(dateTime) : (-1));
			dateTime = ((list.Count < 30) ? default(DateTime) : dateTime);
			return await MergeRemotes(list, dateTime, end, null);
		}
		return false;
	}

	public static Task<bool> PullTimerTimelines(long startPoint, bool resetTime = false, string timerId = null)
	{
		return PullTimerTimelinesCore(startPoint, resetTime, timerId, (string id, long point) => Communicator.GetTimerFocusTimeline(id, point), MergeRemotePomoOfTimer);
	}

	internal static async Task<bool> PullTimerTimelinesCore(long startPoint, bool resetTime, string timerId, Func<string, long, Task<List<PomodoroModel>>> pull, Func<List<PomodoroModel>, DateTime, DateTime, string, Task<bool>> merge)
	{
		Func<bool> isCurrentUser = LocalSettings.Settings.CaptureFocusSyncGuard();
		if (string.IsNullOrEmpty(timerId))
		{
			return false;
		}
		await TimerPullLock.WaitAsync();
		try
		{
			if (!isCurrentUser())
			{
				return false;
			}
			_timerPullPoint = ((resetTime || _timerPullPoint == 0L || _timerPullId != timerId) ? Utils.GetNowTimeStampInMills() : _timerPullPoint);
			_timerPullId = timerId;
			if (_timerPullPoint < 0 || _timerPullPoint < startPoint)
			{
				return false;
			}
			long point = _timerPullPoint;
			List<PomodoroModel> list = await pull(timerId, point);
			if (list != null && isCurrentUser())
			{
				DateTime arg = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).AddMilliseconds(point);
				list = list.OrderByDescending((PomodoroModel f) => f.StartTime).ToList();
				DateTime dateTime = list.LastOrDefault()?.StartTime ?? default(DateTime);
				long nextPoint = ((list.Count >= 30) ? Utils.GetTimeStamp(dateTime) : (-1));
				dateTime = ((list.Count < 30) ? default(DateTime) : dateTime);
				bool result = await merge(list, dateTime, arg, timerId);
				if (!isCurrentUser())
				{
					return false;
				}
				_timerPullPoint = nextPoint;
				return result;
			}
			return false;
		}
		finally
		{
			TimerPullLock.Release();
		}
	}

	private static async Task HandleDeleted(List<string> deleted)
	{
		if (deleted == null || !deleted.Any())
		{
			return;
		}
		foreach (string pomoId in deleted)
		{
			await PomoDao.DeleteByIdAsync(pomoId);
			await PomoTaskDao.DeleteAllByPomoId(pomoId);
		}
	}

	private static async Task CommitDelete(List<string> delete, bool isPomo)
	{
		if (delete == null || delete.Count == 0)
		{
			return;
		}
		List<string> deleted = new List<string>();
		string log = "delete:";
		foreach (string deletePomoId in delete)
		{
			log = log + deletePomoId + ",";
			if (await Communicator.DeletePomo(deletePomoId, isPomo))
			{
				deleted.Add(deletePomoId);
			}
		}
		UtilLog.Info("CommitDeletePomos: " + log);
		await HandleDeleted(deleted);
	}

	private static async Task CommitChanges(BaseSyncBean<PomodoroModel> pomoSyncBean, bool isPomo)
	{
		BatchUpdateResult result = null;
		if (pomoSyncBean != null && pomoSyncBean.Any())
		{
			result = await Communicator.BatchUpdatePomos(pomoSyncBean, isPomo);
		}
		await HandleCommitResult(result);
	}

	private static async Task HandleCommitResult(BatchUpdateResult result)
	{
		if (result?.Id2error != null && result.Id2error.Count > 0)
		{
			List<PomodoroModel> list = await PomoDao.GetPomoInIds(result.Id2error.Keys.ToList());
			if (list != null && list.Count > 0)
			{
				foreach (PomodoroModel item in list.Where((PomodoroModel p) => result.Id2error.ContainsKey(p.Id) && result.Id2error[p.Id] == "EXISTED"))
				{
					item.SyncStatus = -2;
				}
			}
			await PomoDao.UpdateAllAsync(list);
		}
		Dictionary<string, string> id2Etag = result?.Id2etag;
		if (id2Etag == null || id2Etag.Count <= 0)
		{
			return;
		}
		List<PomodoroModel> list2 = await PomoDao.GetPomoInIds(id2Etag.Keys.ToList());
		if (list2 == null || list2.Count <= 0)
		{
			return;
		}
		foreach (PomodoroModel item2 in list2)
		{
			item2.SyncStatus = 2;
			if (id2Etag.ContainsKey(item2.Id))
			{
				item2.Etag = id2Etag[item2.Id];
			}
		}
		await PomoDao.UpdateAllAsync(list2);
	}

	public static async Task CommitPomodoros()
	{
		List<PomodoroModel> needPostPomos = await PomoDao.GetNeedPostPomos();
		if (needPostPomos == null || needPostPomos.Count <= 0)
		{
			return;
		}
		UtilLog.Info("CommitPomodorosCount: " + needPostPomos.Count);
		await PreparePostCommitStatisticsGuard(needPostPomos);
		foreach (PomodoroModel pomodoroModel in needPostPomos)
		{
			pomodoroModel.Tasks = (await PomoDao.GetPomoTasksByPomoId(pomodoroModel.Id)).ToArray();
		}
		TimerSyncService.ClearPulledKey();
		var (pomoSyncBean, pomoDeleteList) = GetSyncArgs(needPostPomos, isPomo: true);
		await CommitChanges(pomoSyncBean, isPomo: true);
		await CommitDelete(pomoDeleteList, isPomo: true);
		var (pomoSyncBean2, timingDeleteList) = GetSyncArgs(needPostPomos, isPomo: false);
		await CommitChanges(pomoSyncBean2, isPomo: false);
		await CommitDelete(timingDeleteList, isPomo: false);
		await PullStatistics();
		PomoNotifier.NotifyPomoCommit();
	}

	private static (BaseSyncBean<PomodoroModel> syncBean, List<string> deleteList) GetSyncArgs(List<PomodoroModel> pomos, bool isPomo)
	{
		int pomoType = ((!isPomo) ? 1 : 0);
		IEnumerable<PomodoroModel> source = pomos.Where((PomodoroModel pomo) => pomo.Type == pomoType);
		BaseSyncBean<PomodoroModel> item = new BaseSyncBean<PomodoroModel>(source.Where((PomodoroModel pomo) => pomo.SyncStatus == 0), source.Where((PomodoroModel pomo) => pomo.SyncStatus == 1));
		List<string> item2 = (from pomo in source
			where pomo.SyncStatus == -1
			select pomo.Id).ToList();
		return (syncBean: item, deleteList: item2);
	}
}
