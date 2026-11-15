using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using TickTickUtils.Lang;
using ticktick_WPF.Dal;
using ticktick_WPF.Resource;
using ticktick_WPF.Util.ReminderTime;
using ticktick_WPF.Views.Pomo;

namespace ticktick_WPF.Notifier;

public static class TimeChangeNotifier
{
	private static DateTime _currentDate;

	private static Timer _timer;

	public static event EventHandler<EventArgs> DayChanged;

	public static void BeginTimer()
	{
		_timer?.Stop();
		_timer?.Close();
		int num = (61 - DateTime.Now.Second) * 1000;
		_currentDate = DateTime.Now.Date;
		_timer = new Timer(num);
		_timer.Elapsed += delegate
		{
			DateTime now = DateTime.Now;
			_timer.Interval = (61 - now.Second) * 1000;
			Application.Current?.Dispatcher.InvokeAsync((Func<Task>)async delegate
			{
				try
				{
					await ReminderBase.TryRemind(now);
				}
				catch (Exception ex)
				{
					UtilLog.Error(ex);
				}
				if (LocalSettings.Settings.ShowCountDown)
				{
					LocalSettings.Settings.NotifyPropertyChanged("ShowCountDown");
				}
				DataChangedNotifier.NotifyPeriodicCheck();
				CheckDateChanged();
			}, DispatcherPriority.Background);
		};
		_timer.Start();
	}

	private static void CheckDateChanged()
	{
		if (_currentDate.Date != DateTime.Now.Date)
		{
			_currentDate = DateTime.Now.Date;
			LocalSettings.Settings.StatisticsModel?.ClearToday();
			TickFocusManager.ReloadStatistics();
			OnDayChanged();
		}
	}

	private static void OnDayChanged()
	{
		TaskDefaultDao.InitCache();
		TimeChangeNotifier.DayChanged?.Invoke(null, null);
	}
}
