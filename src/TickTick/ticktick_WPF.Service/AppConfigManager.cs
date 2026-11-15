using System;
using System.Threading.Tasks;
using System.Timers;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Service;

public static class AppConfigManager
{
	private static Timer _timer;

	public static void StartPolling()
	{
		int num = LocalSettings.Settings.ExtraSettings.AppConfigInterval * 1000;
		if (_timer == null)
		{
			_timer = new Timer(num);
		}
		else
		{
			_timer.Stop();
			_timer.Interval = num;
		}
		_timer.Elapsed -= TimerOnElapsed;
		_timer.Elapsed += TimerOnElapsed;
		_timer.Start();
	}

	private static void TimerOnElapsed(object sender, ElapsedEventArgs e)
	{
		PerformPullAppConfig();
	}

	public static void StopPolling()
	{
		_timer?.Stop();
	}

	public static async Task PerformPullAppConfig(bool login = false)
	{
		try
		{
			await Communicator.PullAppConfig(login);
		}
		catch (Exception ex)
		{
			UtilLog.Error(ex);
		}
	}
}
