using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TickTickModels;
using TickTickUtils;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Notifier;
using ticktick_WPF.Resource;
using ticktick_WPF.Service;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Util.Sync.Model;

namespace ticktick_WPF.Util.Sync;

public static class HabitSyncService
{
	private static readonly SemaphoreLocker SyncLock;

	private static DateTime _lastSyncTime;

	private static DateTime _lastLaunchSyncTime;

	private static readonly DelayActionHandler _commitHabitHandler;

	private static readonly DelayActionHandler _commitHabitCheckInsHandler;

	static HabitSyncService()
	{
		SyncLock = new SemaphoreLocker();
		_commitHabitHandler = new DelayActionHandler(2000);
		_commitHabitCheckInsHandler = new DelayActionHandler(2000);
		_commitHabitHandler.SetAction(delegate
		{
			BatchUpdateHabit();
		});
		_commitHabitCheckInsHandler.SetAction(delegate
		{
			CommitHabitCheckInsInternal();
		});
	}

	public static async Task SyncHabitCheckInCalendar(DateTime startDate)
	{
		if (LocalSettings.Settings.ShowHabit && startDate <= DateTime.Today)
		{
			DateTime dateTime = ((startDate.AddDays(-1.0) >= DateTime.Today.AddDays(-31.0)) ? DateTime.Today.AddDays(-31.0) : startDate.AddDays(-1.0));
			if (LocalSettings.Settings.HabitInCal)
			{
				PullHabits((int)(DateTime.Today - dateTime).TotalDays);
			}
		}
	}

	public static async Task SyncHabitsOnLogin()
	{
		if (!LocalSettings.Settings.ShowHabit)
		{
			return;
		}
		List<HabitModel> habits = await HabitService.PullRemoteHabits();
		if (habits == null)
		{
			return;
		}
		List<string> habitIds = habits.Select((HabitModel habit) => habit.Id).ToList();
		if (!habitIds.Any())
		{
			return;
		}
		await HabitService.PullRemoteHabitsCheckIn(habitIds);
		await HabitService.PullRemoteHabitsRecord(habitIds);
		await HabitDao.SaveAllHabitCheckStamp(int.Parse(DateTime.Today.AddDays(-91.0).ToString("yyyyMMdd", App.Ci)));
		foreach (HabitModel item in habits)
		{
			await HabitDao.UpdateHabitInfo(item.Id, isUpdateSyncStatus: false);
		}
	}

	public static async Task PullHabits(int days)
	{
		if (!LocalSettings.Settings.ShowHabit)
		{
			return;
		}
		List<HabitModel> habits = await HabitService.PullRemoteHabits(needMerge: false);
		if (habits == null)
		{
			return;
		}
		bool changed = await HabitService.MergeRemoteHabits(habits);
		List<string> habitIds = (from habit in habits
			where habit.Status == 0
			select habit.Id).ToList();
		if (habitIds.Any())
		{
			changed = await HabitService.PullRemoteHabitsCheckIn(habitIds, DateUtils.GetDateNum(DateTime.Today.AddDays(-days))) || changed;
			await HabitService.PullRemoteHabitsRecord(habitIds, DateUtils.GetDateNum(DateTime.Today.AddDays(-days)));
			CommitHabitCheckIns();
		}
		if (!changed)
		{
			return;
		}
		foreach (HabitModel item in habits)
		{
			await HabitDao.UpdateHabitInfo(item.Id, isUpdateSyncStatus: false);
		}
		Application.Current?.Dispatcher?.Invoke(delegate
		{
			DataChangedNotifier.NotifyHabitsChanged();
			if (ABTestManager.IsNewRemindCalculate())
			{
				HabitReminderCalculator.InitHabitReminders();
			}
			else
			{
				ReminderCalculator.AssembleReminders();
			}
		});
	}

	public static async void SyncHabit()
	{
		_lastLaunchSyncTime = DateTime.Now;
		SyncLock.LockAsync(async delegate
		{
			if (_lastSyncTime >= _lastLaunchSyncTime)
			{
				return;
			}
			_lastSyncTime = DateTime.Now;
			try
			{
				await PullHabits(91);
			}
			catch (Exception)
			{
			}
			finally
			{
				Application.Current?.Dispatcher?.Invoke(DataChangedNotifier.NotifyHabitsSyncDone);
			}
		});
	}

	private static async Task BatchUpdateHabit()
	{
		List<HabitModel> list = await HabitDao.GetNeedPostHabits();
		SyncHabitBean syncBean = new SyncHabitBean();
		if (list == null || !list.Any())
		{
			return;
		}
		foreach (HabitModel item in list)
		{
			switch (item.SyncStatus)
			{
			case 0:
				syncBean.Add.Add(item);
				break;
			case 1:
				syncBean.Update.Add(item);
				break;
			case -1:
				syncBean.Deleted.Add(item.Id);
				break;
			}
		}
		if (syncBean.Empty())
		{
			return;
		}
		string text = await Communicator.UpdateHabit(syncBean);
		if (text == null || !text.Contains("id2etag"))
		{
			return;
		}
		try
		{
			BatchUpdateResult model = JsonConvert.DeserializeObject<BatchUpdateResult>(text);
			foreach (string key in model.Id2etag.Keys)
			{
				string etag = model.Id2etag[key];
				await HabitDao.SaveHabitEtag(key, etag);
			}
			foreach (string item2 in syncBean.Deleted)
			{
				HabitModel habitModel = await HabitDao.GetHabitById(item2);
				if (habitModel != null)
				{
					App.Connection.DeleteAsync(habitModel);
				}
			}
			if (model.Id2error == null)
			{
				return;
			}
			foreach (string key2 in model.Id2error.Keys)
			{
				_ = key2;
			}
		}
		catch (Exception)
		{
		}
	}

	public static async void CommitHabits(bool immediately = false)
	{
		if (immediately)
		{
			_commitHabitHandler.ImmediatelyDoAction();
		}
		else
		{
			_commitHabitHandler.TryDoAction();
		}
	}

	public static async Task CommitHabitRecords()
	{
		List<HabitRecordModel> records = await HabitRecordDao.GetNeedPostHabitRecords();
		if (!records.Any())
		{
			return;
		}
		SyncHabitRecordBean bean = new SyncHabitRecordBean();
		List<HabitModel> list = await HabitDao.GetAllHabits();
		List<HabitRecordModel> commitModels = new List<HabitRecordModel>();
		foreach (HabitRecordModel record in records)
		{
			HabitModel habitModel = list?.FirstOrDefault((HabitModel h) => h.Id == record.HabitId);
			if (habitModel == null || habitModel.Status == -1)
			{
				App.Connection.DeleteAsync(record);
				continue;
			}
			commitModels.Add(record);
			switch (record.Status)
			{
			case 0:
				bean.Add.Add(record);
				break;
			case 1:
				if (record.Deleted == 1)
				{
					bean.Delete.Add(record);
				}
				else
				{
					bean.Update.Add(record);
				}
				break;
			}
		}
		string text = await Communicator.CommitHabitRecords(bean);
		if (text != null)
		{
			try
			{
				await HabitRecordDao.HandleRecordCommitResult(JsonConvert.DeserializeObject<BatchUpdateResult>(text), commitModels);
			}
			catch (Exception ex)
			{
				UtilLog.Warn("CommitHabitRecords Failed, " + ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
	}

	public static void CommitHabitCheckIns(bool immediately = false)
	{
		if (immediately)
		{
			_commitHabitCheckInsHandler.ImmediatelyDoAction();
		}
		else
		{
			_commitHabitCheckInsHandler.TryDoAction();
		}
	}

	private static async Task CommitHabitCheckInsInternal()
	{
		List<HabitCheckInModel> checkIns = await HabitCheckInDao.GetNeedPostHabitCheckIns();
		if (checkIns == null || !checkIns.Any())
		{
			return;
		}
		UtilLog.Info("CommitCheckIns " + checkIns.Aggregate("", (string text2, HabitCheckInModel checkIn) => text2 + $"{checkIn.HabitId},{checkIn.Value},{checkIn.CheckStatus},{checkIn.CheckinStamp}\r\n"));
		SyncHabitCheckInBean bean = new SyncHabitCheckInBean();
		HashSet<string> addedCheckIns = new HashSet<string>();
		foreach (HabitCheckInModel item2 in checkIns)
		{
			if (item2.Status != -1)
			{
				string item = item2.HabitId + item2.CheckinStamp;
				if (addedCheckIns.Contains(item))
				{
					await HabitCheckInDao.DeleteAsync(item2);
					continue;
				}
				addedCheckIns.Add(item2.HabitId + item2.CheckinStamp);
			}
			switch (item2.Status)
			{
			case 0:
				bean.Add.Add(item2);
				break;
			case 1:
				bean.Update.Add(item2);
				break;
			case -1:
				bean.Delete.Add(item2);
				break;
			}
		}
		string result = await Communicator.CommitHabitCheckIns(bean);
		UtilLog.Info("CommitCheckInsResult : " + result);
		if (result != null && result.Contains("id2etag"))
		{
			try
			{
				await HabitCheckInDao.HandleCommitResult(JsonConvert.DeserializeObject<BatchUpdateResult>(result), checkIns);
			}
			catch (Exception ex)
			{
				UtilLog.Warn("BatchUpdateHabitCheckIns Failed, " + ExceptionUtils.BuildExceptionMessage(ex));
			}
		}
		if (result == null || !result.Contains("error"))
		{
			return;
		}
		try
		{
			if (!string.IsNullOrEmpty(JsonConvert.DeserializeObject<ErrorModel>(result)?.errorId) && JObject.Parse(result).TryGetValue("data", out JToken value))
			{
				string text = value.First?.First?.ToString();
				if (text != null)
				{
					await HabitDao.CheckHabitValid(text);
				}
			}
		}
		catch (Exception ex2)
		{
			UtilLog.Warn("BatchUpdateHabitCheckIns Failed, " + ExceptionUtils.BuildExceptionMessage(ex2));
		}
	}

	public static void DelaySync()
	{
		DelayActionHandlerCenter.TryDoAction("DelaySyncHabitsAndSections", delegate
		{
			SyncHabit();
			Utils.RunOnUiThread(App.Window.Dispatcher, HabitSectionsSyncService.SyncSections);
		}, 3000);
	}

	public static async Task CommitHabitAndCheckIns()
	{
		await BatchUpdateHabit();
		CommitHabitRecords();
		await CommitHabitCheckInsInternal();
	}
}
