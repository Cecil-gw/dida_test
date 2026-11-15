using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.ReminderTime;

internal static class FullscreenReminderGuard
{
	private static readonly object Locker = new object();

	private static readonly List<ReminderModel> PendingReminders = new List<ReminderModel>();

	private static DispatcherTimer _flushTimer;

	private static bool _flushing;

	public static bool TryDefer(ReminderModel reminder)
	{
		if (reminder == null || !ForegroundFullscreenWindowDetector.IsOtherApplicationFullscreen())
		{
			return false;
		}
		lock (Locker)
		{
			ReminderModel reminderModel = reminder.Copy();
			string key = GetQueueKey(reminderModel);
			int num = PendingReminders.FindIndex((ReminderModel item) => GetQueueKey(item) == key);
			if (num >= 0)
			{
				PendingReminders[num] = reminderModel;
			}
			else
			{
				PendingReminders.Add(reminderModel);
			}
		}
		EnsureFlushTimer();
		UtilLog.Info($"ReminderDeferredByFullscreen {reminder.Type} {reminder.GetObjId()}");
		return true;
	}

	private static void EnsureFlushTimer()
	{
		(Application.Current?.Dispatcher)?.InvokeAsync(delegate
		{
			if (_flushTimer == null)
			{
				_flushTimer = new DispatcherTimer(DispatcherPriority.Background)
				{
					Interval = TimeSpan.FromSeconds(1.0)
				};
				_flushTimer.Tick += OnFlushTimerTick;
			}
			if (!_flushTimer.IsEnabled)
			{
				_flushTimer.Start();
			}
		});
	}

	private static async void OnFlushTimerTick(object sender, EventArgs e)
	{
		if (_flushing || ForegroundFullscreenWindowDetector.IsOtherApplicationFullscreen())
		{
			return;
		}
		List<ReminderModel> list;
		lock (Locker)
		{
			if (PendingReminders.Count == 0)
			{
				_flushTimer?.Stop();
				return;
			}
			list = PendingReminders.Select((ReminderModel reminder) => reminder.Copy()).ToList();
			PendingReminders.Clear();
		}
		_flushing = true;
		_flushTimer?.Stop();
		try
		{
			foreach (ReminderModel item in list)
			{
				await ReminderBase.ShowReminderWindow(item);
			}
		}
		finally
		{
			_flushing = false;
			lock (Locker)
			{
				if (PendingReminders.Count > 0)
				{
					EnsureFlushTimer();
				}
			}
		}
	}

	private static string GetQueueKey(ReminderModel reminder)
	{
		string text = reminder?.GetObjId();
		if (!string.IsNullOrEmpty(text))
		{
			return $"{reminder.Type}:{text}";
		}
		return $"{reminder?.Type}:{reminder?.Id}:{reminder?.GroupId}";
	}
}
