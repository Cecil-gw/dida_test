using System;
using System.Timers;

namespace ticktick_WPF.Util;

public class DelayActionHandler
{
	private bool _cleared;

	private readonly object _locker = new object();

	private readonly Timer _delayTimer = new Timer
	{
		AutoReset = false
	};

	public event EventHandler DoAction;

	public DelayActionHandler(int interval = 300)
	{
		_delayTimer.Interval = interval;
		_delayTimer.Elapsed += OnAction;
	}

	private void OnAction(object sender, ElapsedEventArgs e)
	{
		lock (_locker)
		{
			_delayTimer.Stop();
		}
		this.DoAction?.Invoke(this, null);
	}

	public void ImmediatelyDoAction()
	{
		lock (_locker)
		{
			_delayTimer.Stop();
		}
		this.DoAction?.Invoke(this, null);
	}

	public void TryDoAction(int? interval = null)
	{
		lock (_locker)
		{
			try
			{
				if (_delayTimer != null)
				{
					_delayTimer.Interval = ((double?)interval) ?? _delayTimer.Interval;
					if (!_delayTimer.Enabled)
					{
						_delayTimer.Start();
					}
				}
			}
			catch
			{
			}
		}
	}

	public void CancelAction()
	{
		lock (_locker)
		{
			_delayTimer.Stop();
		}
	}

	public void SetAction(EventHandler handler)
	{
		lock (_locker)
		{
			this.DoAction = null;
			_delayTimer.Stop();
			if (_cleared)
			{
				_cleared = false;
				_delayTimer.Elapsed += OnAction;
			}
			DoAction += handler;
		}
	}

	public void StopAndClear()
	{
		lock (_locker)
		{
			_cleared = true;
			this.DoAction = null;
			_delayTimer.Stop();
			_delayTimer.Elapsed -= OnAction;
		}
	}

	public void Dispose()
	{
		lock (_locker)
		{
			_cleared = true;
			this.DoAction = null;
			_delayTimer.Stop();
			_delayTimer.Elapsed -= OnAction;
			_delayTimer.Close();
		}
	}
}
