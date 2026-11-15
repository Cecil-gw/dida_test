using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ticktick_WPF.Framework;

public static class ScrollViewerAnimationHelper
{
	private class AnimationState
	{
		public DispatcherTimer Timer { get; set; }

		public CancellationTokenSource CancelToken { get; set; }

		public DateTime StartTime { get; set; }

		public TimeSpan Duration { get; set; }

		public double StartVertical { get; set; }

		public double TargetVertical { get; set; }

		public double StartHorizontal { get; set; }

		public double TargetHorizontal { get; set; }

		public EasingMode Easing { get; set; }

		public EventHandler TickHandler { get; set; }
	}

	public enum EasingMode
	{
		Linear,
		EaseOut,
		EaseIn,
		EaseInOut
	}

	private static readonly Dictionary<ScrollViewer, AnimationState> _animationMap = new Dictionary<ScrollViewer, AnimationState>();

	public static void SmoothScrollToBottom(this ScrollViewer scrollViewer, double durationSeconds = 0.3, EasingMode easing = EasingMode.EaseOut)
	{
		if (scrollViewer != null)
		{
			double scrollableHeight = scrollViewer.ScrollableHeight;
			if (double.IsInfinity(scrollableHeight) || scrollableHeight <= 0.0)
			{
				scrollViewer.ScrollToBottom();
			}
			else
			{
				scrollViewer.SmoothScrollToVerticalOffset(scrollableHeight, durationSeconds, easing);
			}
		}
	}

	public static void SmoothScrollToVerticalOffset(this ScrollViewer scrollViewer, double offset, double durationSeconds = 0.3, EasingMode easing = EasingMode.EaseOut)
	{
		if (scrollViewer != null)
		{
			double targetV = Math.Max(0.0, Math.Min(scrollViewer.ScrollableHeight, offset));
			SmoothAnimate(scrollViewer, scrollViewer.VerticalOffset, targetV, scrollViewer.HorizontalOffset, scrollViewer.HorizontalOffset, durationSeconds, easing);
		}
	}

	public static void SmoothScrollToHorizontalOffset(this ScrollViewer scrollViewer, double offset, double durationSeconds = 0.3, EasingMode easing = EasingMode.EaseOut)
	{
		if (scrollViewer != null)
		{
			double targetH = Math.Max(0.0, Math.Min(scrollViewer.ScrollableWidth, offset));
			SmoothAnimate(scrollViewer, scrollViewer.VerticalOffset, scrollViewer.VerticalOffset, scrollViewer.HorizontalOffset, targetH, durationSeconds, easing);
		}
	}

	public static void SmoothScrollTo(this ScrollViewer scrollViewer, double horizontalOffset, double verticalOffset, double durationSeconds = 0.3, EasingMode easing = EasingMode.EaseOut)
	{
		if (scrollViewer != null)
		{
			double targetV = Math.Max(0.0, Math.Min(scrollViewer.ScrollableHeight, verticalOffset));
			double targetH = Math.Max(0.0, Math.Min(scrollViewer.ScrollableWidth, horizontalOffset));
			SmoothAnimate(scrollViewer, scrollViewer.VerticalOffset, targetV, scrollViewer.HorizontalOffset, targetH, durationSeconds, easing);
		}
	}

	public static void CancelScrollAnimation(this ScrollViewer scrollViewer)
	{
		if (scrollViewer != null && _animationMap.TryGetValue(scrollViewer, out var value))
		{
			value.CancelToken.Cancel();
			value.Timer.Stop();
			CleanupAnimationState(scrollViewer);
		}
	}

	private static void SmoothAnimate(ScrollViewer scrollViewer, double startV, double targetV, double startH, double targetH, double durationSeconds, EasingMode easing)
	{
		if (scrollViewer == null)
		{
			return;
		}
		TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
		if (duration.TotalMilliseconds < 1.0)
		{
			throw new ArgumentException("Duration must be greater than 0.", "durationSeconds");
		}
		scrollViewer.CancelScrollAnimation();
		CancellationTokenSource cts = new CancellationTokenSource();
		DispatcherTimer timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(16.0)
		};
		AnimationState state = new AnimationState
		{
			Timer = timer,
			CancelToken = cts,
			StartTime = DateTime.Now,
			Duration = duration,
			StartVertical = startV,
			TargetVertical = targetV,
			StartHorizontal = startH,
			TargetHorizontal = targetH,
			Easing = easing
		};
		_animationMap[scrollViewer] = state;
		EventHandler eventHandler = delegate
		{
			try
			{
				if (!cts.IsCancellationRequested && scrollViewer.IsAlive())
				{
					double num = Math.Min(1.0, (DateTime.Now - state.StartTime).TotalSeconds / state.Duration.TotalSeconds);
					double num2 = Ease(num, state.Easing);
					double offset = startV + (targetV - startV) * num2;
					double offset2 = startH + (targetH - startH) * num2;
					scrollViewer.ScrollToVerticalOffset(offset);
					scrollViewer.ScrollToHorizontalOffset(offset2);
					if (num >= 1.0)
					{
						scrollViewer.ScrollToVerticalOffset(targetV);
						scrollViewer.ScrollToHorizontalOffset(targetH);
						timer.Stop();
						CleanupAnimationState(scrollViewer);
					}
				}
			}
			catch
			{
				timer.Stop();
				CleanupAnimationState(scrollViewer);
			}
		};
		state.TickHandler = eventHandler;
		timer.Tick += eventHandler;
		timer.Start();
	}

	private static double Ease(double progress, EasingMode mode)
	{
		switch (mode)
		{
		case EasingMode.EaseOut:
			return 1.0 - Math.Pow(1.0 - progress, 3.0);
		case EasingMode.EaseIn:
			return Math.Pow(progress, 3.0);
		case EasingMode.EaseInOut:
			if (progress < 0.5)
			{
				return 4.0 * Math.Pow(progress, 3.0);
			}
			return 1.0 - Math.Pow(-2.0 * progress + 2.0, 3.0) / 2.0;
		default:
			return progress;
		}
	}

	private static void CleanupAnimationState(ScrollViewer scrollViewer)
	{
		if (!_animationMap.TryGetValue(scrollViewer, out var value))
		{
			return;
		}
		try
		{
			DispatcherTimer timer = value.Timer;
			EventHandler tickHandler = value.TickHandler;
			if (timer != null && tickHandler != null)
			{
				timer.Stop();
				timer.Tick -= tickHandler;
			}
		}
		finally
		{
			value.CancelToken?.Dispose();
			_animationMap.Remove(scrollViewer);
		}
	}

	private static bool IsAlive(this ScrollViewer sv)
	{
		if (sv != null)
		{
			return sv.Parent != null;
		}
		return false;
	}
}
