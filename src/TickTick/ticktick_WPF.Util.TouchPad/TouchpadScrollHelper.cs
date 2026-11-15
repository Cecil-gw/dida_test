using System;
using System.Windows.Controls;

namespace ticktick_WPF.Util.TouchPad;

internal sealed class TouchpadScrollHelper
{
	private readonly ScrollViewer _scrollViewer;

	private readonly Func<DateTime> _getNow;

	private DateTime? _lastTouchpadInput;

	private double? _pendingOffset;

	public TouchpadScrollHelper(ScrollViewer scrollViewer, Func<DateTime> getNow = null)
	{
		_scrollViewer = scrollViewer;
		_getNow = getNow ?? ((Func<DateTime>)(() => DateTime.UtcNow));
		scrollViewer.ScrollChanged += delegate(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange != 0.0 || e.ExtentHeightChange != 0.0 || e.ViewportHeightChange != 0.0)
			{
				_pendingOffset = null;
			}
		};
	}

	public void OnDoubleFingerTouch()
	{
		_lastTouchpadInput = _getNow();
	}

	public bool TryScroll(int delta)
	{
		if (delta == 0)
		{
			return true;
		}
		DateTime dateTime = _getNow();
		bool flag = _lastTouchpadInput.HasValue && (dateTime - _lastTouchpadInput.Value).TotalMilliseconds < 200.0;
		if (!flag && delta % 120 == 0)
		{
			_pendingOffset = null;
			return false;
		}
		_lastTouchpadInput = dateTime;
		double num = ((!flag) ? _scrollViewer.VerticalOffset : (_pendingOffset ?? _scrollViewer.VerticalOffset));
		_pendingOffset = Math.Max(0.0, Math.Min(_scrollViewer.ScrollableHeight, num - (double)delta));
		_scrollViewer.ScrollToVerticalOffset(_pendingOffset.Value);
		return true;
	}
}
