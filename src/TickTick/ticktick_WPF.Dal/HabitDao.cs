using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Sync;

namespace ticktick_WPF.Dal;

public static class HabitDao
{
	public static async Task<List<HabitModel>> GetNeedCheckHabits(bool checkSkip = true)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<HabitModel> list = await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && v.Status == 0
			orderby v.SortOrder
			select v).ToListAsync();
		if (checkSkip)
		{
			list = list.Where((HabitModel habit) => !habit.IsSkipToday()).ToList();
		}
		foreach (HabitModel item in list)
		{
			HandleHabitValue(item);
		}
		return list;
	}

	private static void HandleHabitValue(HabitModel habit)
	{
		if (habit != null)
		{
			habit.Type = habit.GetSafeType();
			if (habit.Unit == "Count" && Utils.IsCn())
			{
				habit.Unit = Utils.GetString("Count");
			}
		}
	}

	private static List<HabitModel> HandleHabitValues(List<HabitModel> habits)
	{
		habits?.ForEach(HandleHabitValue);
		return habits;
	}

	public static async Task<List<HabitModel>> GetAllHabits()
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return HandleHabitValues(await (from it in App.Connection.Table<HabitModel>()
			where it.UserId == userId
			orderby it.SortOrder
			select it).ToListAsync());
	}

	public static async Task<List<HabitModel>> GetHabitsbyStatus(int status)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		return HandleHabitValues(await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && v.Status == status
			select v).ToListAsync());
	}

	public static async Task<List<HabitModel>> GetHabitsBySectionId(string sectionId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		List<HabitModel> list = ((!(sectionId == HabitSectionModel.GetDefault().Id) && !string.IsNullOrEmpty(sectionId)) ? (await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && v.SectionId == sectionId
			orderby v.SortOrder
			select v).ToListAsync()) : (await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && (v.SectionId == sectionId || v.SectionId == null || v.SectionId == "-1")
			orderby v.SortOrder
			select v).ToListAsync()));
		if (list != null && list.Any())
		{
			foreach (HabitModel item in list)
			{
				HandleHabitValue(item);
			}
		}
		return list;
	}

	public static async Task<long> GetNewHabitSortOrderBySectionId(string sectionId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		HabitModel habitModel = ((!(sectionId == HabitSectionModel.GetDefault().Id) && !string.IsNullOrEmpty(sectionId)) ? (await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && v.SectionId == sectionId
			orderby v.SortOrder
			select v).FirstOrDefaultAsync()) : (await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && (v.SectionId == sectionId || v.SectionId == null || v.SectionId == "-1")
			orderby v.SortOrder
			select v).FirstOrDefaultAsync()));
		if (habitModel != null)
		{
			return habitModel.SortOrder - 268435456;
		}
		return 0L;
	}

	public static async Task<HabitModel> GetHabitById(string habitId)
	{
		string userId = Utils.GetCurrentUserIdInt().ToString();
		HabitModel habitModel = await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && v.Id == habitId
			select v).FirstOrDefaultAsync();
		if (habitModel != null)
		{
			HandleHabitValue(habitModel);
		}
		return habitModel;
	}

	public static async Task<HabitModel> GetFirstHabit()
	{
		return (await HabitService.GetHabitsbyStatus(0)).OrderBy((HabitModel it) => it.SortOrder).FirstOrDefault();
	}

	public static async Task SaveHabitCheckStamp(string habitId, int stamp)
	{
		HabitModel habitModel = await GetHabitById(habitId);
		if (habitModel != null)
		{
			habitModel.CheckStamp = stamp;
			await UpdateAsync(habitModel);
		}
	}

	public static async Task<int> GetHabitTotalCheckCount(string habitId)
	{
		return (await HabitCheckInDao.GetHabitCheckInsByHabitId(habitId))?.Select((HabitCheckInModel checkIn) => checkIn.CheckinStamp).Distinct().Count() ?? 0;
	}

	public static async Task UpdateHabitInfo(string habitId, bool isUpdateSyncStatus = true)
	{
		HabitModel habit = await GetHabitById(habitId);
		if (habit != null)
		{
			HabitModel habitModel = habit;
			habitModel.CompletedCyclesList = await GetHabitCompletedCycles(habit);
			habit.CompletedCycles = habit.CompletedCyclesList?.Count((CompletedCycle cycle) => cycle.isComplete) ?? 0;
			habitModel = habit;
			habitModel.TotalCheckIns = await GetHabitTotalCheckCount(habit.Id);
			if (isUpdateSyncStatus)
			{
				habit.ModifiedTime = DateTime.Now;
				habit.SyncStatus = ((habit.SyncStatus != 0) ? 1 : 0);
			}
			await UpdateAsync(habit);
		}
	}

	public static async Task<List<CompletedCycle>> GetHabitCompletedCycles(HabitModel habit)
	{
		List<CompletedCycle> cycle = new List<CompletedCycle>();
		List<HabitCheckInModel> list = await HabitCheckInDao.GetHabitCheckInsByHabitId(habit.Id);
		if (habit.TargetDays > 0 && habit.TargetStartDate.HasValue)
		{
			CompletedCycle completedCycle = new CompletedCycle
			{
				StartDate = habit.TargetStartDate.GetValueOrDefault(),
				TargetDays = habit.TargetDays.GetValueOrDefault()
			};
			int num = DateUtils.DateTimeToInt(DateTime.Today);
			if (list != null && list.Any())
			{
				foreach (int item in from checkinStamp in (from checkIn in list
						where checkIn.IsComplete()
						select int.Parse(checkIn.CheckinStamp)).Distinct()
					orderby checkinStamp
					select checkinStamp)
				{
					if (!(item >= habit.TargetStartDate) || item > num)
					{
						continue;
					}
					completedCycle.CheckinDays.Add(item);
					if (completedCycle.Streak == completedCycle.TargetDays)
					{
						int num2 = (completedCycle.EndDate = item);
						cycle.Add(completedCycle);
						if (num2 == num)
						{
							break;
						}
						DateTime time = DateUtils.IntToDateTime(num2).AddDays(1.0);
						completedCycle = new CompletedCycle
						{
							StartDate = DateUtils.DateTimeToInt(time),
							TargetDays = completedCycle.TargetDays
						};
					}
				}
			}
			if (completedCycle.EndDate != num)
			{
				completedCycle.EndDate = num;
				cycle.Add(completedCycle);
			}
		}
		return cycle;
	}

	public static async Task<HabitModel> SaveHabitTotalCount(string habitId)
	{
		HabitModel habit = await GetHabitById(habitId);
		if (habit != null)
		{
			habit.TotalCheckIns = await GetHabitTotalCheckCount(habitId);
			HabitModel habitModel = habit;
			habitModel.CompletedCyclesList = await GetHabitCompletedCycles(habit);
			List<CompletedCycle> completedCyclesList = habit.CompletedCyclesList;
			habit.CompletedCycles = ((completedCyclesList != null) ? new int?(completedCyclesList.Count((CompletedCycle item) => item.isComplete)) : habit.CompletedCycles);
			await UpdateAsync(habit);
			return habit;
		}
		return null;
	}

	public static async Task SaveHabitEtag(string habitId, string etag)
	{
		HabitModel habitModel = await GetHabitById(habitId);
		if (habitModel != null)
		{
			habitModel.Etag = etag;
			habitModel.SyncStatus = 2;
			await UpdateAsync(habitModel);
		}
	}

	public static async Task SaveAllHabitCheckStamp(int stamp)
	{
		List<HabitModel> list = await GetAllHabits();
		if (list.Any())
		{
			list.ForEach(delegate(HabitModel habit)
			{
				habit.CheckStamp = stamp;
			});
			await UpdateAllAsync(list);
		}
	}

	public static async Task<List<long>> GetAllSortOrder(string sectionId)
	{
		return (await GetHabitsBySectionId(sectionId))?.Select((HabitModel h) => h.SortOrder).ToList();
	}

	public static async Task ChangeHabitCheckStyle(string habitId, int style)
	{
		HabitModel habitModel = await GetHabitById(habitId);
		if (habitModel != null)
		{
			habitModel.ModifiedTime = DateTime.Now;
			habitModel.Style = style;
			habitModel.SyncStatus = ((habitModel.SyncStatus != 0) ? 1 : 0);
			await UpdateAsync(habitModel);
		}
	}

	public static async Task ChangeHabitArchiveStatus(string habitId, bool isArchive)
	{
		HabitModel habit = await GetHabitById(habitId);
		if (habit != null)
		{
			habit.Status = (isArchive ? 1 : 0);
			habit.ModifiedTime = DateTime.Now;
			habit.ArchivedTime = (isArchive ? new DateTime?(DateTime.Now) : ((DateTime?)null));
			List<long> list = await GetAllSortOrder(habit.SectionId);
			long num = ((list != null && list.Count > 0) ? list.Min() : 0);
			if (habit.SortOrder != num)
			{
				habit.SortOrder = num - 268435456;
			}
			habit.SyncStatus = ((habit.SyncStatus != 0) ? 1 : 0);
			await UpdateAsync(habit);
			if (ABTestManager.IsNewRemindCalculate())
			{
				await HabitReminderCalculator.RecalHabitReminder(habit.Id);
			}
			else
			{
				ReminderCalculator.AssembleReminders();
			}
		}
	}

	public static async Task DeleteHabit(string habitId)
	{
		HabitModel habitModel = await GetHabitById(habitId);
		if (habitModel != null)
		{
			UtilLog.Info("HabitDao.DeleteHabit  " + habitId);
			habitModel.Status = -1;
			habitModel.ModifiedTime = DateTime.Now;
			habitModel.SyncStatus = -1;
			await UpdateAsync(habitModel);
			await HabitCheckInDao.DeleteCheckInsByHabitId(habitId);
			await HabitRecordDao.DeleteRecordsByHabitId(habitId);
			DataChangedNotifier.NotifyHabitsChanged();
			HabitSyncService.CommitHabits();
		}
	}

	public static async void SaveSortOrderAndSection(string habitId, long sortOrder, string sectionId)
	{
		HabitModel habitModel = await GetHabitById(habitId);
		if (habitModel != null)
		{
			habitModel.SortOrder = sortOrder;
			habitModel.ModifiedTime = DateTime.Now;
			habitModel.SectionId = sectionId;
			habitModel.SyncStatus = ((habitModel.SyncStatus != 0) ? 1 : 0);
			await UpdateAsync(habitModel);
			DataChangedNotifier.NotifyHabitsChanged();
			HabitSyncService.CommitHabits();
		}
	}

	public static async Task HabitUnableRecord(string habitId)
	{
		HabitModel habitModel = await GetHabitById(habitId);
		if (habitModel != null)
		{
			habitModel.RecordEnable = false;
			habitModel.ModifiedTime = DateTime.Now;
			habitModel.SyncStatus = ((habitModel.SyncStatus != 0) ? 1 : 0);
			await UpdateAsync(habitModel);
			HabitSyncService.CommitHabits();
		}
	}

	public static async Task<int> InsertAllAsync(List<HabitModel> model)
	{
		return await App.Connection.InsertAllAsync(model);
	}

	public static async Task<int> UpdateAsync(HabitModel model)
	{
		return await App.Connection.UpdateAsync(model);
	}

	public static async Task<int> UpdateAllAsync(List<HabitModel> model)
	{
		return await App.Connection.UpdateAllAsync(model);
	}

	public static async Task<List<HabitModel>> GetNeedPostHabits()
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && v.SyncStatus != 2
			select v).ToListAsync();
	}

	public static async Task CheckHabitValid(string habitId)
	{
		HabitModel habitModel = await GetHabitById(habitId);
		if (habitModel == null || habitModel.SyncStatus != 0 || habitModel.Status == -1)
		{
			await HabitCheckInDao.DeleteCheckInsByHabitId(habitId);
			await HabitRecordDao.DeleteRecordsByHabitId(habitId);
		}
	}

	public static async Task<List<SkipHabitModel>> GetAllSkipHabitModels()
	{
		return await (from model in App.Connection.Table<SkipHabitModel>()
			where model.UserId == LocalSettings.Settings.LoginUserId
			select model).ToListAsync();
	}

	public static async Task<List<HabitModel>> GetHabitByIds(List<string> ids)
	{
		string userId = LocalSettings.Settings.LoginUserId;
		return HandleHabitValues(await (from v in App.Connection.Table<HabitModel>()
			where v.UserId == userId && ids.Contains(v.Id)
			select v).ToListAsync());
	}

	public static async Task<string> GetHabitNameById(string id)
	{
		string sql = "select Name as Text from HabitModel where Id = '" + id + "' limit 1";
		StringModel stringModel = (await App.Connection.QueryAsync<StringModel>(sql, Array.Empty<object>()))?.FirstOrDefault();
		return (stringModel != null) ? (stringModel.Text ?? string.Empty) : null;
	}
}
