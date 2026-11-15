using System;
using System.Timers;

namespace ticktick_WPF.Util;

public class TimeCheckActionHandler
{
	private DateTime? _actionTime;

	private readonly Timer _delayTimer;

	private readonly int _interval;

	public event EventHandler DoAction;

	public TimeCheckActionHandler(int interval = 3)
	{
		_delayTimer = new Timer(interval * 1000);
		_delayTimer.Elapsed -= OnAction;
		_delayTimer.Elapsed += OnAction;
		_interval = interval;
	}

	private void OnAction(object sender, ElapsedEventArgs e)
	{
		_delayTimer.Stop();
		this.DoAction?.Invoke(this, null);
	}

	public void TryDoAction()
	{
		if (CheckCanDoAction())
		{
			_delayTimer.Stop();
			this.DoAction?.Invoke(this, null);
		}
		else
		{
			_delayTimer.Stop();
			_delayTimer.Start();
		}
	}

	private bool CheckCanDoAction()
	{
		if (!_actionTime.HasValue)
		{
			_actionTime = DateTime.Now;
			return true;
		}
		if ((DateTime.Now - _actionTime.Value).TotalSeconds > (double)_interval)
		{
			_actionTime = DateTime.Now;
			return true;
		}
		return false;
	}
}
