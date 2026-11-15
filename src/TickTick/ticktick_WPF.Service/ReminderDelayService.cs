using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TickTickDao;
using TickTickModels;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;
using ticktick_WPF.Util.ReminderTime;

namespace ticktick_WPF.Service;

public static class ReminderDelayService
{
	public static async Task UploadDelayModels()
	{
		List<ReminderDelayModel> models = (await ReminderDelayDao.GetNeedSyncModelsAsync()).Where((ReminderDelayModel m) => m.SyncStatus == -1 || (m.SyncStatus == 0 && m.NextTime > DateTime.Now)).ToList();
		if (models.Count == 0)
		{
			return;
		}
		ReminderDelaySyncModel reminderDelaySyncModel = new ReminderDelaySyncModel();
		foreach (ReminderDelayModel item in models)
		{
			switch (item.SyncStatus)
			{
			case 0:
				reminderDelaySyncModel.add.Add(item);
				break;
			case -1:
				reminderDelaySyncModel.delete.Add(item.ObjId);
				break;
			}
		}
		if (await Communicator.UpdateReminderDelayModels(reminderDelaySyncModel))
		{
			foreach (ReminderDelayModel item2 in models)
			{
				switch (item2.SyncStatus)
				{
				case 0:
					item2.SyncStatus = 2;
					await BaseDao<ReminderDelayModel>.UpdateAsync(item2);
					break;
				case -1:
					await BaseDao<ReminderDelayModel>.DeleteAsync(item2);
					break;
				}
			}
		}
		else
		{
			UtilLog.Info("UpdateReminderDelayModels error");
		}
	}

	public static async Task MergeRemoteDelayModels(List<ReminderDelayModel> remotes)
	{
		if (remotes == null)
		{
			return;
		}
		List<ReminderDelayModel> list = await ReminderDelayDao.GetAllModelsAsync();
		Dictionary<string, ReminderDelayModel> localDict = new Dictionary<string, ReminderDelayModel>();
		foreach (ReminderDelayModel item in list)
		{
			if ((item.SyncStatus != -1 && item.NextTime < DateTime.Now) || string.IsNullOrEmpty(item.ObjId) || localDict.ContainsKey(item.ObjId))
			{
				await BaseDao<ReminderDelayModel>.DeleteAsync(item);
			}
			else
			{
				localDict[item.ObjId] = item;
			}
		}
		bool courseChanged = false;
		foreach (ReminderDelayModel remote in remotes)
		{
			if (!string.IsNullOrEmpty(remote.ObjId) && localDict.ContainsKey(remote.ObjId))
			{
				ReminderDelayModel reminderDelayModel = localDict[remote.ObjId];
				if (reminderDelayModel.SyncStatus == -1 || reminderDelayModel.SyncStatus == 0)
				{
					continue;
				}
				localDict.Remove(remote.ObjId);
				if (reminderDelayModel.RemindTime == remote.RemindTime && reminderDelayModel.NextTime == remote.NextTime)
				{
					continue;
				}
				await BaseDao<ReminderDelayModel>.DeleteAsync(reminderDelayModel);
			}
			remote.SyncStatus = 2;
			remote.UserId = LocalSettings.Settings.LoginUserId;
			await BaseDao<ReminderDelayModel>.InsertAsync(remote);
			await ReloadReminder(remote);
			courseChanged = courseChanged || remote.Type == "course";
		}
		foreach (ReminderDelayModel remote in localDict.Values)
		{
			if (remote.SyncStatus == 2)
			{
				await BaseDao<ReminderDelayModel>.DeleteAsync(remote);
				await ReloadReminder(remote);
				courseChanged = courseChanged || remote.Type == "course";
			}
		}
		if (courseChanged)
		{
			await CourseReminderCalculator.InitCourseReminders();
		}
	}

	private static async Task ReloadReminder(ReminderDelayModel delay)
	{
		string type = delay.Type;
		if (!(type == "calendar"))
		{
			if (type == "habit")
			{
				await HabitReminderCalculator.RecalHabitReminder(delay.ObjId);
			}
			return;
		}
		CalendarEventModel calendarEventModel = await CalendarEventDao.GetEventById(delay.ObjId);
		if (calendarEventModel == null)
		{
			calendarEventModel = await CalendarEventDao.GetEventByStartTimeArchiveId(delay.RemindTime, delay.ObjId);
		}
		await EventReminderCalculator.RecalEventReminders(calendarEventModel);
	}
}
