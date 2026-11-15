using System;
using System.Timers;

namespace ticktick_WPF.Util;

public class Debouncer : IDisposable
{
	private readonly Timer _timer;

	private readonly object _lock = new object();

	private Action _action;

	public Debouncer(int delayMilliseconds, Action action)
	{
		_action = action ?? throw new ArgumentNullException("action");
		_timer = new Timer(delayMilliseconds)
		{
			AutoReset = false
		};
		_timer.Elapsed += OnTimerElapsed;
	}

	private void OnTimerElapsed(object sender, ElapsedEventArgs e)
	{
		_action?.Invoke();
	}

	public void Trigger()
	{
		lock (_lock)
		{
			_timer.Stop();
			_timer.Start();
		}
	}

	public void Dispose()
	{
		lock (_lock)
		{
			_timer?.Stop();
			_timer?.Dispose();
			_action = null;
		}
	}
}
