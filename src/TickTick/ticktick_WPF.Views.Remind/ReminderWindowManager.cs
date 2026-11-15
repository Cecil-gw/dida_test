using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ticktick_WPF.Models;
using ticktick_WPF.Views.Habit;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Views.Remind;

public class ReminderWindowManager
{
	private static List<IRemindPop> _reminderWindowSortLists;

	private static List<IRemindPop> _reminderWindowLists;

	static ReminderWindowManager()
	{
		_reminderWindowSortLists = new List<IRemindPop>();
		_reminderWindowLists = new List<IRemindPop>();
		SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
	}

	public static async Task TryAddNewWindow(ReminderModel reminder)
	{
		string key = (reminder.IsAnnoyingAlert ? reminder.GetReminderKey() : string.Empty);
		IRemindPop remindPop = ((!string.IsNullOrEmpty(key)) ? _reminderWindowLists.FirstOrDefault((IRemindPop window) => window.ReminderKey == key) : _reminderWindowLists.FirstOrDefault((IRemindPop window) => window.IsSameReminder(reminder)));
		if (remindPop != null)
		{
			remindPop.UpdateReminder(reminder);
			PromoteToFront(remindPop);
		}
		else if (reminder.Type == ReminderModel.RemindType.Habit)
		{
			await AddHabitWindow(reminder);
		}
		else
		{
			AddWindow(reminder);
		}
	}

	private static async Task AddHabitWindow(ReminderModel reminder)
	{
		HabitReminderWindow window = new HabitReminderWindow();
		window.Closed += OnPopupClosed;
		_reminderWindowSortLists.Add(window);
		_reminderWindowLists.Add(window);
		await window.InitData(reminder);
		if (window.IsClosed)
		{
			_reminderWindowSortLists.Remove(window);
			_reminderWindowLists.Remove(window);
		}
		else
		{
			window.ShowWindow();
			SetReminderWindowsStyle();
		}
	}

	private static void AddWindow(ReminderModel reminder)
	{
		ReminderPopup reminderPopup = new ReminderPopup(reminder);
		reminderPopup.Closed += OnPopupClosed;
		_reminderWindowSortLists.Add(reminderPopup);
		_reminderWindowLists.Add(reminderPopup);
		reminderPopup.ShowWindow();
		SetReminderWindowsStyle();
	}

	public static void AddPomoReminder(bool relax, bool auto)
	{
		PomoReminderWindow pomoReminderWindow = new PomoReminderWindow();
		pomoReminderWindow.Closed += OnPopupClosed;
		pomoReminderWindow.IsRelax = relax;
		pomoReminderWindow.IsAutomatic = auto;
		pomoReminderWindow.SetIcon();
		CloseAllPomoReminders();
		_reminderWindowSortLists.Add(pomoReminderWindow);
		_reminderWindowLists.Add(pomoReminderWindow);
		SetReminderWindowsStyle();
		pomoReminderWindow.ShowWindow();
	}

	public static void PromoteToFront(ReminderModel reminder)
	{
		string key = reminder.GetReminderKey();
		if (!string.IsNullOrEmpty(key))
		{
			IRemindPop remindPop = _reminderWindowLists.FirstOrDefault((IRemindPop window) => window.ReminderKey == key);
			if (remindPop != null)
			{
				PromoteToFront(remindPop);
			}
		}
	}

	public static void OnWindowMoved(IRemindPop rem)
	{
		if (_reminderWindowSortLists.Contains(rem))
		{
			_reminderWindowSortLists.Remove(rem);
			SetReminderWindowsStyle();
		}
	}

	private static void OnPopupClosed(object sender, EventArgs e)
	{
		if (sender is IRemindPop item)
		{
			_reminderWindowSortLists.Remove(item);
			_reminderWindowLists.Remove(item);
		}
		SetReminderWindowsStyle();
	}

	private static void SetReminderWindowsStyle(bool refreshHorizontalPosition = false)
	{
		List<IRemindPop> list = _reminderWindowSortLists.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (refreshHorizontalPosition && list[i] is Window window)
			{
				ReminderWindowPositionHelper.RefreshHorizontalPosition(window);
			}
			if (list.Count - i <= 3)
			{
				list[i].SetDisplayStyle(list.Count - i - 1);
			}
			else
			{
				list[i].TryHide();
			}
		}
	}

	private static void OnDisplaySettingsChanged(object sender, EventArgs e)
	{
		Application.Current?.Dispatcher?.InvokeAsync(delegate
		{
			SetReminderWindowsStyle(refreshHorizontalPosition: true);
		});
	}

	public static void CloseAllPomoReminders()
	{
		foreach (IRemindPop item in _reminderWindowLists.ToList())
		{
			if (item is PomoReminderWindow pomoReminderWindow)
			{
				pomoReminderWindow.TryClose();
			}
		}
	}

	private static void PromoteToFront(IRemindPop reminder)
	{
		_reminderWindowSortLists.Remove(reminder);
		_reminderWindowSortLists.Add(reminder);
		SetReminderWindowsStyle();
		reminder.ShowWindow();
	}
}
