using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapster;
using TickTickDao;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Cache;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public class PomoService
{
	public static async Task DeleteById(string pomoId)
	{
		PomodoroModel pomo = await PomoDao.GetPomoById(pomoId);
		if (pomo != null)
		{
			if (LocalSettings.Settings.StatisticsModel != null)
			{
				StatisticsModel statisticsDelta = GetStatisticsDelta(await GetAdjustPomo(pomo), -1);
				LocalSettings.Settings.StatisticsModel.Add(statisticsDelta);
			}
			pomo.SyncStatus = -1;
			await PomoDao.UpdateAsync(pomo);
			await TaskService.DeletePomodoro(pomoId, (await PomoDao.GetPomoTasksByPomoId(pomo.Id)).ToList(), pomo.Type == 0, isDelete: true);
			PomoNotifier.NotifyChanged(pomo.Id, PomoChangeType.Deleted);
			SyncManager.TryDelaySync();
		}
	}

	public static async Task DeleteByIds(List<string> pomoIds)
	{
		List<PomodoroModel> pomos = await PomoDao.GetPomoInIds(pomoIds);
		if (pomos != null)
		{
			IEnumerable<string> pomodoroIds = from x in pomos
				where x.Type == 0
				select x.Id;
			IEnumerable<string> timingIds = from x in pomos
				where x.Type == 1
				select x.Id;
			await Communicator.DeleteMultiPomoRec(pomodoroIds, timingIds);
			await DeletePomoFocusInLocal(pomos);
		}
	}

	public static async Task<bool> DeleteAllPomoFocusRec()
	{
		List<PomodoroModel> pomos = await PomoDao.GetAllPomo();
		if (pomos == null)
		{
			return true;
		}
		bool success = await Communicator.DeleteAllPomoRec();
		if (success)
		{
			await DeletePomoFocusInLocal(pomos);
		}
		return success;
	}

	private static async Task DeletePomoFocusInLocal(IEnumerable<PomodoroModel> pomos)
	{
		foreach (PomodoroModel pomo in pomos)
		{
			if (LocalSettings.Settings.StatisticsModel != null)
			{
				StatisticsModel statisticsDelta = GetStatisticsDelta(await GetAdjustPomo(pomo), -1);
				LocalSettings.Settings.StatisticsModel.Add(statisticsDelta);
			}
			pomo.SyncStatus = -1;
			List<PomoTask> source = await PomoDao.GetPomoTasksByPomoId(pomo.Id);
			await TaskService.DeletePomodoro(pomo.Id, source.ToList(), pomo.Type == 0, isDelete: true);
		}
		await PomoDao.UpdateAllAsync(pomos);
		PomoNotifier.NotifyChanged(pomos.Select((PomodoroModel x) => x.Id).ToList(), PomoChangeType.Deleted);
		SyncManager.TryDelaySync();
	}

	public static async Task<PomoStat> LoadStatistics(bool isPomo)
	{
		PomoStat statInfo = new PomoStat();
		DateTime todayStart = DateTime.Today;
		DateTime todayEnd = todayStart.AddDays(1.0);
		DateTime weekStart = Utils.GetWeekStart(todayStart);
		List<PomodoroModel> source = await PomoDao.GetPomoByDateSpan(weekStart, todayEnd);
		List<PomodoroModel> list = source.Where((PomodoroModel p) => IsFocusInRange(p, todayStart, todayEnd)).ToList();
		List<PomodoroModel> weeks = source.Where((PomodoroModel p) => IsFocusInRange(p, weekStart, todayEnd)).ToList();
		statInfo.TodayPomos = list.Count((PomodoroModel p) => p.Type == 0 && p.StartTime.Date == todayStart);
		statInfo.WeeklyPomos = weeks.Count((PomodoroModel p) => p.Type == 0 && p.StartTime.Date >= weekStart);
		statInfo.TodayDuration = 0L;
		foreach (PomodoroModel item in list)
		{
			long focusDurationInRange = GetFocusDurationInRange(await GetAdjustPomo(item), todayStart, todayEnd);
			if (focusDurationInRange > 0)
			{
				statInfo.TodayDuration += focusDurationInRange;
			}
		}
		statInfo.WeeklyDuration = 0L;
		foreach (PomodoroModel item2 in weeks)
		{
			long focusDurationInRange2 = GetFocusDurationInRange(await GetAdjustPomo(item2), weekStart, todayEnd);
			if (focusDurationInRange2 > 0)
			{
				statInfo.WeeklyDuration += focusDurationInRange2;
			}
		}
		return statInfo;
	}

	public static async Task RebindLatestPomoTask(string pomoId, string focusId, int focusType)
	{
		if (string.IsNullOrEmpty(pomoId))
		{
			return;
		}
		PomodoroModel pomo = await PomoDao.GetPomoById(pomoId);
		if (pomo == null)
		{
			UtilLog.Error("RebindLatestPomoTask: pomo id '" + pomoId + "' not found");
			return;
		}
		TimerModel timer = await GetFocusTimer(focusId, focusType);
		if (focusType == 2 && timer == null)
		{
			return;
		}
		string newTaskId = string.Empty;
		string newHabitId = string.Empty;
		if (!string.IsNullOrEmpty(focusId))
		{
			switch (focusType)
			{
			case 1:
				newHabitId = ((timer?.ObjType == "habit") ? timer.ObjId : focusId);
				break;
			case 2:
				if (timer?.ObjType == "habit")
				{
					newHabitId = timer.ObjId;
				}
				else if (timer?.ObjType == "task")
				{
					newTaskId = timer.ObjId;
				}
				break;
			default:
				newTaskId = ((timer?.ObjType == "task") ? timer.ObjId : focusId);
				break;
			}
		}
		PomoTask latestRawTask = (await PomoDao.GetPomoTasksByPomoId(pomoId))?.OrderBy((PomoTask task) => task.EndTime).ThenBy((PomoTask task) => task.StartTime).LastOrDefault();
		List<PomoTask> list = await GetSummaryPomoTasksForRebind(pomo);
		PomoTask pomoTask = (from task in list
			orderby task.EndTime, task.StartTime
			select task).LastOrDefault();
		string oldTaskId = ((!string.IsNullOrEmpty(pomoTask?.TaskId)) ? pomoTask.TaskId : latestRawTask?.TaskId);
		DateTime startDate = pomoTask?.StartTime ?? latestRawTask?.StartTime ?? pomo.StartTime;
		await RebindTaskIdOfPomoTask(pomoId, startDate, newTaskId, newHabitId, timer, list, pomoTask);
		PomoNotifier.NotifyChanged(pomoId, PomoChangeType.Updated);
		TaskChangeNotifier.NotifyTaskPomoAdded(new List<string> { oldTaskId, newTaskId }.Where((string id) => !string.IsNullOrEmpty(id)).Distinct().ToList());
	}

	private static async Task<List<PomoTask>> GetSummaryPomoTasksForRebind(PomodoroModel pomo)
	{
		List<PomoTask> list = ((!pomo.IsAdjust) ? (await PomoDao.GetPomoTasksByPomoId(pomo.Id)) : (await GetAdjustPomoTasks(pomo.Id, pomo)))?.Select((PomoTask task) => task.Copy()).ToList() ?? new List<PomoTask>();
		if (list.Any())
		{
			return list;
		}
		PomodoroModel pomodoroModel = ((!pomo.IsAdjust) ? pomo : (await GetAdjustPomo(pomo)));
		PomodoroModel pomodoroModel2 = pomodoroModel;
		return new List<PomoTask>
		{
			new PomoTask
			{
				PomoId = pomo.Id,
				StartTime = pomo.StartTime,
				EndTime = (pomodoroModel2?.EndTime ?? pomo.EndTime)
			}
		};
	}

	private static async Task<TimerModel> GetFocusTimer(string focusId, int focusType)
	{
		if (string.IsNullOrEmpty(focusId))
		{
			return null;
		}
		return (focusType != 2) ? (await TimerDao.GetTimerByObjId(focusId)) : (await TimerDao.GetTimerById(focusId));
	}

	public static async Task RebindTaskIdOfPomoTask(string pomoId, DateTime startDate, string newTaskId, string newHabitId, TimerModel timer, List<PomoTask> summaryPomoTasks = null, PomoTask summaryPomoTask = null)
	{
		PomodoroModel pomo = await PomoDao.GetPomoById(pomoId);
		if (pomo == null)
		{
			return;
		}
		List<PomoTask> pomoTasks = await PomoDao.GetPomoTasksByPomoId(pomoId);
		if (pomoTasks == null || pomoTasks.Count == 0)
		{
			PomoTask task = new PomoTask
			{
				StartTime = startDate,
				EndTime = pomo.EndTime,
				PomoId = pomoId
			};
			await PomoTaskDao.InsertAsync(task);
			pomoTasks = new List<PomoTask> { task };
		}
		PomoTask pomoTask = pomoTasks?.FirstOrDefault((PomoTask p) => p.StartTime == startDate);
		if (pomoTask == null || (!string.IsNullOrEmpty(newTaskId) && TaskCache.GetTaskById(newTaskId) == null))
		{
			return;
		}
		HabitModel habitModel = ((!string.IsNullOrEmpty(newHabitId)) ? (await HabitDao.GetHabitById(newHabitId)) : null);
		HabitModel newHabit = habitModel;
		if (!string.IsNullOrEmpty(newHabitId) && newHabit == null)
		{
			return;
		}
		TimerModel timerModel = (string.IsNullOrEmpty(pomoTask.TimerSid) ? (await TimerDao.GetTimerByObjId(string.IsNullOrEmpty(pomoTask.TaskId) ? pomoTask.HabitId : pomoTask.TaskId)) : (await TimerDao.GetTimerById(pomoTask.TimerSid)));
		TimerModel originTimer = timerModel;
		PomoTask summaryTask = summaryPomoTask ?? pomoTask;
		List<PomoTask> pomoTasks2 = summaryPomoTasks ?? pomoTasks;
		long summaryDuration = Utils.GetTotalSecond(summaryTask.StartTime, summaryTask.EndTime);
		string oldHabitId = ((!string.IsNullOrEmpty(summaryTask.HabitId)) ? summaryTask.HabitId : pomoTask.HabitId);
		string text = summaryTask.TaskId ?? string.Empty;
		string targetTaskId = newTaskId ?? string.Empty;
		if ((!string.IsNullOrEmpty(targetTaskId) || !string.IsNullOrEmpty(text)) && text != targetTaskId)
		{
			await TaskService.UpdateTaskPomoSummary(text, targetTaskId, summaryTask, pomoTasks2, pomo);
			if (!string.IsNullOrEmpty(targetTaskId) && summaryTask != pomoTask)
			{
				pomoTask.ProjectName = summaryTask.ProjectName;
				pomoTask.TagString = summaryTask.TagString;
				pomoTask.Title = summaryTask.Title;
			}
		}
		if (!string.IsNullOrEmpty(newHabitId))
		{
			pomoTask.ProjectName = string.Empty;
			pomoTask.TagString = string.Empty;
			pomoTask.Title = newHabit.Name;
		}
		await RebindHabitPomoProgress(oldHabitId, newHabitId, summaryDuration, summaryTask.StartTime.Date);
		pomoTask.TaskId = newTaskId;
		pomoTask.HabitId = newHabitId;
		pomoTask.TimerSid = timer?.Id;
		pomoTask.TimerName = timer?.Name;
		if (string.IsNullOrEmpty(newTaskId) && string.IsNullOrEmpty(newHabitId))
		{
			pomoTask.Tags = null;
			pomoTask.TagString = null;
			pomoTask.ProjectName = null;
			pomoTask.Title = timer?.Name;
		}
		await PomoDao.UpdatePomoTask(pomoTask);
		PomoNotifier.NotifyLinkChanged(originTimer?.Id, timer?.Id);
		if (pomo.SyncStatus == 2)
		{
			pomo.SyncStatus = 1;
		}
		if (Utils.IsEmptyDate(pomo.StartTime))
		{
			UtilLog.Info("PomoService.RebindTaskIdOfPomoTask : " + pomo.Id);
			return;
		}
		await PomoDao.UpdateAsync(pomo);
		SyncManager.TryDelaySync();
	}

	private static async Task RebindHabitPomoProgress(string oldHabitId, string newHabitId, long second, DateTime date)
	{
		if (!(oldHabitId == newHabitId))
		{
			await RollbackHabitPomoProgress(oldHabitId, second, date);
			if (!string.IsNullOrEmpty(newHabitId))
			{
				await HabitService.OnPomoTaskChanged(newHabitId, second, date);
			}
		}
	}

	private static async Task RollbackHabitPomoProgress(string habitId, long second, DateTime date)
	{
		if (string.IsNullOrEmpty(habitId))
		{
			return;
		}
		HabitModel habitModel = await HabitDao.GetHabitById(habitId);
		if (habitModel == null || habitModel.IsBoolHabit())
		{
			return;
		}
		double step = 0.0;
		if (FocusConstance.HourUnit.Contains(habitModel.Unit.Trim().ToLower()))
		{
			step = (double)second / 3600.0;
		}
		if (FocusConstance.MinuteUnit.Contains(habitModel.Unit.Trim().ToLower()))
		{
			step = (double)second / 60.0;
		}
		step = Math.Round(step, 2);
		if (!(step <= 0.0))
		{
			HabitCheckInModel habitCheckInModel = await HabitCheckInDao.GetHabitCheckInByHabitIdAndStamp(habitId, ticktick_WPF.Util.DateUtils.GetDateNum(date).ToString());
			if (habitCheckInModel != null)
			{
				double step2 = Math.Max(0.0, habitCheckInModel.Value - step);
				await HabitService.CheckInHabit(habitId, date, step2, increment: false, checkShowRecord: false);
			}
		}
	}

	public static async Task SaveNote(string pomoId, string note)
	{
		PomodoroModel pomodoroModel = await PomoDao.GetPomoById(pomoId);
		if (pomodoroModel != null && !(pomodoroModel.Note == note))
		{
			pomodoroModel.Note = note;
			if (pomodoroModel.SyncStatus == 2)
			{
				pomodoroModel.SyncStatus = 1;
			}
			if (Utils.IsEmptyDate(pomodoroModel.StartTime))
			{
				UtilLog.Info("PomoService.SaveNote : " + pomodoroModel.Id);
				return;
			}
			await PomoDao.UpdateAsync(pomodoroModel);
			SyncManager.TryDelaySync();
		}
	}

	public static async Task SaveFocusModel(PomodoroModel pomo)
	{
		if (await PomoDao.GetPomoById(pomo.Id) != null)
		{
			NotifyFocusSaved(pomo);
			return;
		}
		await PomoTaskDao.DeleteAllByPomoId(pomo.Id);
		PomoTask[] tasks = pomo.Tasks;
		if (tasks != null && tasks.Length != 0)
		{
			List<PomoTask> tasks2 = pomo.Tasks.ToList();
			await App.Connection.InsertAllAsync(tasks2);
			if (tasks2.Count > 0)
			{
				await TaskService.UpdatePomodoro(pomo.Id, tasks2.Where((PomoTask po) => !string.IsNullOrEmpty(po.TaskId) || !string.IsNullOrEmpty(po.HabitId)).ToList(), pomo.Type == 0);
			}
		}
		await PomoDao.InsertAsync(pomo);
		try
		{
			if (LocalSettings.Settings.StatisticsModel != null)
			{
				StatisticsModel statisticsDelta = GetStatisticsDelta(pomo, 1);
				LocalSettings.Settings.StatisticsModel.Add(statisticsDelta);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
		UtilLog.Info("SaveFocusModel: " + pomo.Id + "," + Utils.GetSafeDateLogText(pomo.StartTime) + "," + Utils.GetSafeDateLogText(pomo.EndTime) + "," + pomo.Tasks?.Length);
		NotifyFocusSaved(pomo);
	}

	private static void NotifyFocusSaved(PomodoroModel pomo)
	{
		try
		{
			SyncManager.TryDelaySync(1200);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
		try
		{
			PomoNotifier.NotifyChanged(pomo.Id, PomoChangeType.Added);
		}
		catch (Exception ex2)
		{
			UtilLog.Error(ex2);
		}
		List<string> list = (from task in pomo.Tasks?.Where((PomoTask task) => task != null && !string.IsNullOrEmpty(task.TaskId))
			select task.TaskId).Distinct().ToList();
		if (list != null && list.Count > 0)
		{
			try
			{
				TaskChangeNotifier.NotifyTaskPomoAdded(list);
			}
			catch (Exception ex3)
			{
				UtilLog.Error(ex3);
			}
		}
	}

	public static async Task UpdateLastPomoDuration(string pomoId, long adjustTime)
	{
		try
		{
			if (string.IsNullOrEmpty(pomoId))
			{
				UtilLog.Error("UpdateLastPomoDuration: pomoId is empty");
				return;
			}
			PomodoroModel pomo = await PomoDao.GetPomoById(pomoId);
			if (pomo == null)
			{
				UtilLog.Error("UpdateLastPomoDuration: pomo id '" + pomoId + "' not found");
				return;
			}
			bool shouldUpdateStatistics = LocalSettings.Settings?.StatisticsModel != null;
			StatisticsModel statisticsModel = ((!shouldUpdateStatistics) ? null : GetStatisticsDelta(await GetAdjustPomo(pomo), 1));
			StatisticsModel oldStatistics = statisticsModel;
			pomo.AdjustTime = adjustTime;
			pomo.SyncStatus = 1;
			await PomoDao.UpdateAsync(pomo);
			Math.Max(0L, Utils.GetTotalSecond(pomo.StartTime, pomo.EndTime) - pomo.PauseDuration);
			_ = adjustTime / 1000;
			List<PomoTask> rawTasks = await PomoDao.GetPomoTasksByPomoId(pomoId);
			List<PomoTask> tasks = await GetAdjustPomoTasks(pomoId, pomo);
			if (tasks.Any())
			{
				foreach (IGrouping<string, PomoTask> group in from x in tasks
					where !string.IsNullOrEmpty(x.TaskId)
					group x by x.TaskId)
				{
					string taskId = group.Key;
					PomodoroSummaryModel pomodoroSummaryModel = await PomoSummaryDao.GetPomoByTaskId(taskId);
					if (pomodoroSummaryModel?.focuses == null)
					{
						continue;
					}
					object[] array = pomodoroSummaryModel.focuses.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoId);
					long num = group.Sum((PomoTask x) => Utils.GetTotalSecond(x.StartTime, x.EndTime));
					if (array != null)
					{
						if (array[2].ToString() == num.ToString())
						{
							continue;
						}
						array[2] = num;
					}
					else
					{
						array = new object[3] { pomoId, 0, num };
						pomodoroSummaryModel.focuses.Add(array);
					}
					await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel);
					await SyncStatusDao.AddModifySyncStatus(taskId);
				}
			}
			IEnumerable<string> enumerable = rawTasks.Select((PomoTask p) => p.TaskId).Except(tasks.Select((PomoTask p) => p.TaskId));
			if (enumerable.Any())
			{
				foreach (string taskId in enumerable)
				{
					PomodoroSummaryModel pomodoroSummaryModel2 = await PomoSummaryDao.GetPomoByTaskId(taskId);
					if (pomodoroSummaryModel2?.focuses != null)
					{
						object[] array2 = pomodoroSummaryModel2.focuses.FirstOrDefault((object[] f) => f.Length == 3 && f[0] as string == pomoId);
						if (array2 != null)
						{
							pomodoroSummaryModel2.focuses.Remove(array2);
						}
						await PomoSummaryDao.SavePomoSummary(pomodoroSummaryModel2);
					}
					await SyncStatusDao.AddModifySyncStatus(taskId);
				}
			}
			if (shouldUpdateStatistics && LocalSettings.Settings?.StatisticsModel != null)
			{
				StatisticsModel statisticsDelta = GetStatisticsDelta(await GetAdjustPomo(pomo), 1);
				StatisticsModel statisticsModel2 = new StatisticsModel();
				statisticsModel2.totalPomoDuration += statisticsDelta.totalPomoDuration - oldStatistics.totalPomoDuration;
				statisticsModel2.todayPomoDuration += statisticsDelta.todayPomoDuration - oldStatistics.todayPomoDuration;
				LocalSettings.Settings.StatisticsModel.Add(statisticsModel2);
			}
			PomoNotifier.NotifyChanged(pomo.Id, PomoChangeType.Updated);
			TaskChangeNotifier.NotifyTaskPomoAdded(new List<string> { pomo.Id });
			SyncManager.TryDelaySync(1200);
		}
		catch (Exception ex)
		{
			UtilLog.Error("UpdateLastPomoDuration:" + ex.Message + Environment.NewLine + ex.StackTrace);
		}
	}

	public static async Task<PomodoroModel> GetAdjustPomo(string pomoId)
	{
		return await GetAdjustPomo(await PomoDao.GetPomoById(pomoId));
	}

	public static async Task<PomodoroModel> GetAdjustPomo(PomodoroModel pomo)
	{
		if (pomo == null)
		{
			return null;
		}
		if (!pomo.IsAdjust)
		{
			pomo.Tasks = (await PomoDao.GetPomoTasksByPomoId(pomo.Id)).ToArray();
			return pomo;
		}
		PomodoroModel newPomo = pomo.Adapt<PomodoroModel>();
		List<PomoTask> list = await GetAdjustPomoTasks(pomo.Id, pomo);
		double num = 0.0;
		for (int i = 0; i < list.Count - 1; i++)
		{
			DateTime startTime = list[i + 1].StartTime;
			DateTime endTime = list[i].EndTime;
			if (!(startTime < endTime))
			{
				num += (startTime - endTime).TotalSeconds;
			}
		}
		newPomo.PauseDuration = (long)num;
		newPomo.EndTime = pomo.StartTime.AddMilliseconds(pomo.AdjustTime.Value).AddSeconds(num);
		newPomo.Tasks = list.ToArray();
		return newPomo;
	}

	public static async Task<List<PomoTask>> GetAdjustPomoTasks(string pomoId, PomodoroModel pomo = null)
	{
		if (pomo == null)
		{
			pomo = await PomoDao.GetPomoById(pomoId);
		}
		List<PomoTask> list = await PomoDao.GetPomoTasksByPomoId(pomoId);
		if (!list.Any())
		{
			return list;
		}
		if (!pomo.IsAdjust)
		{
			return list;
		}
		list = list.OrderBy((PomoTask x) => x.StartTime).ToList();
		long num = Math.Max(0L, Utils.GetTotalSecond(pomo.StartTime, pomo.EndTime) - pomo.PauseDuration);
		long num2 = pomo.AdjustTime.Value / 1000 - num;
		if (num2 > 0)
		{
			PomoTask pomoTask = list.LastOrDefault();
			pomoTask.EndTime = pomoTask.EndTime.AddSeconds(num2);
		}
		else
		{
			num2 = Math.Abs(num2);
			for (int num3 = list.Count - 1; num3 >= 0; num3--)
			{
				PomoTask pomoTask2 = list[num3];
				long totalSecond = Utils.GetTotalSecond(pomoTask2.StartTime, pomoTask2.EndTime);
				if (totalSecond > num2)
				{
					pomoTask2.EndTime = pomoTask2.EndTime.AddSeconds(-num2);
					break;
				}
				list.RemoveAt(num3);
				num2 -= totalSecond;
				if (num2 <= 0)
				{
					break;
				}
			}
		}
		return list;
	}

	public static async Task<PomodoroSummaryModel> GetAdjustPomoSummary(string taskId)
	{
		return await PomoSummaryDao.GetPomoByTaskId(taskId);
	}

	public static StatisticsModel GetStatisticsModel(List<PomodoroModel> models, bool checkDelete)
	{
		StatisticsModel statisticsModel = new StatisticsModel();
		long num = 0L;
		long num2 = 0L;
		foreach (PomodoroModel model in models)
		{
			if (model.SyncStatus == 1)
			{
				continue;
			}
			int num3 = ((!checkDelete || model.SyncStatus != -1) ? 1 : (-1));
			if (model.Type == 0)
			{
				statisticsModel.totalPomoCount += num3;
				if (model.EndTime > DateTime.Today)
				{
					statisticsModel.todayPomoCount += num3;
				}
			}
			long num4 = num3 * GetFocusDurationInRange(model, DateTime.MinValue, DateTime.MaxValue);
			num += num4;
			if (model.EndTime > DateTime.Today)
			{
				num2 += num3 * GetFocusDurationInRange(model, DateTime.Today, DateTime.Today.AddDays(1.0));
			}
		}
		statisticsModel.todayPomoDuration = SignedSecondToMinute(num2);
		statisticsModel.totalPomoDuration = SignedSecondToMinute(num);
		return statisticsModel;
	}

	public static StatisticsModel GetStatisticsDelta(PomodoroModel pomo, int rate)
	{
		StatisticsModel statisticsModel = new StatisticsModel();
		if (pomo == null || rate == 0)
		{
			return statisticsModel;
		}
		if (pomo.Type == 0)
		{
			statisticsModel.totalPomoCount += rate;
			if (pomo.EndTime > DateTime.Today)
			{
				statisticsModel.todayPomoCount += rate;
			}
		}
		statisticsModel.totalPomoDuration += rate * SignedSecondToMinute(GetFocusDurationInRange(pomo, DateTime.MinValue, DateTime.MaxValue));
		if (pomo.EndTime > DateTime.Today)
		{
			statisticsModel.todayPomoDuration += rate * SignedSecondToMinute(GetFocusDurationInRange(pomo, DateTime.Today, DateTime.Today.AddDays(1.0)));
		}
		return statisticsModel;
	}

	private static int SignedSecondToMinute(long second)
	{
		return ((second >= 0) ? 1 : (-1)) * Utils.SecondToMinute(Math.Abs(second));
	}

	public static long GetFocusDurationInRange(PomodoroModel pomo, DateTime rangeStart, DateTime rangeEnd)
	{
		if (!IsFocusInRange(pomo, rangeStart, rangeEnd))
		{
			return 0L;
		}
		PomoTask[] tasks = pomo.Tasks;
		if (tasks != null && tasks.Length != 0)
		{
			return pomo.Tasks.Where((PomoTask task) => task.TaskId != "StartPause").Sum((PomoTask task) => GetOverlapSeconds(task.StartTime, task.EndTime, rangeStart, rangeEnd));
		}
		long overlapSeconds = GetOverlapSeconds(pomo.StartTime, pomo.EndTime, rangeStart, rangeEnd);
		if (overlapSeconds <= 0)
		{
			return 0L;
		}
		long totalSecond = Utils.GetTotalSecond(pomo.StartTime, pomo.EndTime);
		if (totalSecond <= 0 || pomo.PauseDuration <= 0)
		{
			return overlapSeconds;
		}
		if (rangeStart <= pomo.StartTime && rangeEnd >= pomo.EndTime)
		{
			return Math.Max(0L, overlapSeconds - pomo.PauseDuration);
		}
		long num = (long)Math.Round((double)pomo.PauseDuration * ((double)overlapSeconds / (double)totalSecond), 0, MidpointRounding.AwayFromZero);
		return Math.Max(0L, overlapSeconds - num);
	}

	public static bool IsFocusInRange(PomodoroModel pomo, DateTime rangeStart, DateTime rangeEnd)
	{
		if (pomo != null && rangeStart < rangeEnd && pomo.StartTime < rangeEnd)
		{
			return pomo.EndTime > rangeStart;
		}
		return false;
	}

	private static long GetOverlapSeconds(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd)
	{
		DateTime dateTime = ((start > rangeStart) ? start : rangeStart);
		DateTime dateTime2 = ((end < rangeEnd) ? end : rangeEnd);
		if (!(dateTime2 > dateTime))
		{
			return 0L;
		}
		return Utils.GetTotalSecond(dateTime, dateTime2);
	}

	public static async Task RemovePomoTaskTimerId(string timerId)
	{
		string query = "Update TimerModel Set TimerSid = null where TimerSid = '" + timerId + "'";
		await Connection.DbConnection.ExecuteAsync(query);
	}

	public static async Task UpdatePomoTask(TaskModel task)
	{
		List<PomoTask> list = await PomoDao.GetPomoTasksByTaskId(task.id);
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (PomoTask item in list)
		{
			item.Title = task.title;
		}
		await PomoDao.UpdateAllPomoTask(list);
		SyncManager.TryDelaySync();
	}
}
