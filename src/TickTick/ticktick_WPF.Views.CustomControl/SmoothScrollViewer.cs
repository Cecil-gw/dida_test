using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.CustomControl;

public class SmoothScrollViewer : ScrollViewer
{
	private double LastLocation;

	private DateTime _lastScrollTime;

	public void HandleSmoothMouseWheel(MouseWheelEventArgs e)
	{
		Mouse.OverrideCursor = Cursors.Arrow;
		DelayActionHandlerCenter.TryDoAction("SmoothScrollViewer.HandleSmoothMouseWheel", delegate
		{
			base.Dispatcher.Invoke(delegate
			{
				Mouse.OverrideCursor = null;
			});
		});
		DateTime now = DateTime.Now;
		if ((now - _lastScrollTime).TotalMilliseconds > 200.0)
		{
			LastLocation = base.VerticalOffset;
		}
		_lastScrollTime = now;
		if (Math.Abs(e.Delta) < 60)
		{
			LastLocation -= e.Delta;
			BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);
			ScrollToVerticalOffset(LastLocation);
			e.Handled = true;
			return;
		}
		double num = e.Delta;
		double num2 = LastLocation - num * 0.8;
		ScrollToVerticalOffset(LastLocation);
		if (num2 < 0.0)
		{
			num2 = 0.0;
		}
		if (num2 > base.ScrollableHeight)
		{
			num2 = base.ScrollableHeight;
		}
		AnimateScroll(num2);
		LastLocation = num2;
		e.Handled = true;
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		HandleSmoothMouseWheel(e);
	}

	private void AnimateScroll(double ToValue)
	{
		BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);
		DoubleAnimation doubleAnimation = new DoubleAnimation();
		doubleAnimation.From = base.VerticalOffset;
		doubleAnimation.To = ToValue;
		doubleAnimation.Duration = TimeSpan.FromMilliseconds(150.0);
		BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, doubleAnimation);
	}
}
