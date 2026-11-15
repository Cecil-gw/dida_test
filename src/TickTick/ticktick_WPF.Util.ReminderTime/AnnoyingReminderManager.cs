using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ticktick_WPF.Dal;
using ticktick_WPF.Models;
using ticktick_WPF.Resource;
using ticktick_WPF.Views;

namespace ticktick_WPF.Util.ReminderTime;

public static class AnnoyingReminderManager
{
	private static readonly List<DisplayModule> LegacyEnabledDefaultRemindTypes = new List<DisplayModule>
	{
		DisplayModule.Task,
		DisplayModule.CountDown
	};

	private static readonly object SessionLocker = new object();

	private static readonly Dictionary<string, AnnoyingReminderSession> Sessions = new Dictionary<string, AnnoyingReminderSession>();

	private static readonly TimeSpan RepeatInterval = TimeSpan.FromSeconds(30.0);

	private static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(30.0);

	public static string GetReminderKey(ReminderModel reminder)
	{
		if (reminder == null)
		{
			return string.Empty;
		}
		string arg = reminder.GetObjId() ?? string.Empty;
		return $"{reminder.Type}:{arg}";
	}

	public static string GetReminderKey(RemindMessage remindMessage)
	{
		if (remindMessage == null)
		{
			return string.Empty;
		}
		if (!Enum.TryParse<ReminderModel.RemindType>(remindMessage.type, ignoreCase: true, out var result))
		{
			return string.Empty;
		}
		return $"{result}:{remindMessage.id ?? string.Empty}";
	}

	public static bool ShouldUseAnnoyingAlert(int? entitySetting, DisplayModule module)
	{
		if (!UserDao.IsPro())
		{
			return false;
		}
		switch (entitySetting)
		{
		case 1:
			return true;
		case 0:
			return false;
		default:
		{
			ReminderConfig reminderConfig = LocalSettings.Settings.UserPreference?.Reminder;
			if (reminderConfig == null || !reminderConfig.annoyingAlertEnabled)
			{
				return false;
			}
			List<DisplayModule> effectiveRemindTypes = GetEffectiveRemindTypes(reminderConfig);
			if (effectiveRemindTypes == null || effectiveRemindTypes.Count == 0)
			{
				return false;
			}
			return effectiveRemindTypes.Contains(module);
		}
		}
	}

	public static List<DisplayModule> GetEffectiveRemindTypes(ReminderConfig config)
	{
		if (config == null)
		{
			return null;
		}
		if (config.RemindTypes != null)
		{
			return config.RemindTypes.Distinct().ToList();
		}
		if (!config.annoyingAlertEnabled)
		{
			return null;
		}
		return LegacyEnabledDefaultRemindTypes.ToList();
	}

	public static bool HasActiveSession()
	{
		lock (SessionLocker)
		{
			return Sessions.Count > 0;
		}
	}

	public static void TryStart(ReminderModel reminder, AnnoyingReminderChannel channel)
	{
		if (reminder == null || !reminder.IsAnnoyingAlert)
		{
			return;
		}
		string reminderKey = GetReminderKey(reminder);
		if (string.IsNullOrEmpty(reminderKey))
		{
			return;
		}
		DateTime now = DateTime.Now;
		lock (SessionLocker)
		{
			if (Sessions.TryGetValue(reminderKey, out var value))
			{
				value.Channel = channel;
				value.Reminder = reminder.Copy();
				value.NextNotifyAt = now.Add(RepeatInterval);
			}
			else
			{
				Sessions[reminderKey] = new AnnoyingReminderSession
				{
					Key = reminderKey,
					Reminder = reminder.Copy(),
					Channel = channel,
					StartedAt = now,
					NextNotifyAt = now.Add(RepeatInterval),
					ExpireAt = now.Add(MaxDuration)
				};
			}
		}
		RemindSoundPlayer.StartTaskRemindSoundLoop(reminderKey);
	}

	public static void OnUserHandled(ReminderModel reminder, string reason = "user")
	{
		Stop(GetReminderKey(reminder), reason);
	}

	public static void OnUserHandled(string reminderKey, string reason = "user")
	{
		Stop(reminderKey, reason);
	}

	public static void OnRemoteHandled(RemindMessage remindMessage)
	{
		if (remindMessage == null)
		{
			return;
		}
		string reminderKey = GetReminderKey(remindMessage);
		if (string.IsNullOrEmpty(reminderKey))
		{
			return;
		}
		bool flag = false;
		lock (SessionLocker)
		{
			if (!Sessions.TryGetValue(reminderKey, out var value))
			{
				return;
			}
			DateTime? dateTime = value.Reminder?.ReminderTime;
			if (dateTime.HasValue && remindMessage.remindTime < dateTime.Value)
			{
				return;
			}
			Sessions.Remove(reminderKey);
			flag = true;
		}
		if (flag)
		{
			RemindSoundPlayer.StopTaskRemindSoundLoop(reminderKey);
		}
	}

	public static async Task Tick(DateTime now)
	{
		List<string> expiredKeys = new List<string>();
		List<AnnoyingReminderSession> list;
		lock (SessionLocker)
		{
			list = (from annoyingReminderSession in Sessions.Values
				where annoyingReminderSession.NextNotifyAt <= now
				select annoyingReminderSession.Clone()).ToList();
		}
		foreach (AnnoyingReminderSession session in list)
		{
			lock (SessionLocker)
			{
				if (!Sessions.ContainsKey(session.Key))
				{
					continue;
				}
			}
			if (!(await ReminderBase.ResolveAnnoyingAlert(session.Reminder)))
			{
				expiredKeys.Add(session.Key);
				continue;
			}
			if (session.ExpireAt.HasValue && session.ExpireAt.Value <= now)
			{
				expiredKeys.Add(session.Key);
				continue;
			}
			await ReminderBase.ShowReminderWindow(session.Reminder.Copy());
			lock (SessionLocker)
			{
				if (Sessions.TryGetValue(session.Key, out var value))
				{
					value.NextNotifyAt = now.Add(RepeatInterval);
				}
			}
		}
		foreach (string item in expiredKeys)
		{
			Stop(item, "timeout");
		}
	}

	private static void Stop(string reminderKey, string reason)
	{
		if (!string.IsNullOrEmpty(reminderKey))
		{
			lock (SessionLocker)
			{
				Sessions.Remove(reminderKey);
			}
			RemindSoundPlayer.StopTaskRemindSoundLoop(reminderKey);
		}
	}
}
