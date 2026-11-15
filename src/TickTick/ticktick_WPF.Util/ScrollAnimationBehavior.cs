using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace ticktick_WPF.Util;

public static class ScrollAnimationBehavior
{
	private static ScrollViewer _listBoxScroller = new ScrollViewer();

	public static DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollAnimationBehavior), new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

	public static DependencyProperty TimeDurationProperty = DependencyProperty.RegisterAttached("TimeDuration", typeof(TimeSpan), typeof(ScrollAnimationBehavior), new PropertyMetadata(new TimeSpan(0, 0, 0, 0, 0)));

	public static DependencyProperty PointsToScrollProperty = DependencyProperty.RegisterAttached("PointsToScroll", typeof(double), typeof(ScrollAnimationBehavior), new PropertyMetadata(0.0));

	public static DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ScrollAnimationBehavior), new UIPropertyMetadata(false, OnIsEnabledChanged));

	public static void SetVerticalOffset(FrameworkElement target, double value)
	{
		target.SetValue(VerticalOffsetProperty, value);
	}

	public static double GetVerticalOffset(FrameworkElement target)
	{
		return (double)target.GetValue(VerticalOffsetProperty);
	}

	public static void SetTimeDuration(FrameworkElement target, TimeSpan value)
	{
		target.SetValue(TimeDurationProperty, value);
	}

	public static TimeSpan GetTimeDuration(FrameworkElement target)
	{
		return (TimeSpan)target.GetValue(TimeDurationProperty);
	}

	public static void SetPointsToScroll(FrameworkElement target, double value)
	{
		target.SetValue(PointsToScrollProperty, value);
	}

	public static double GetPointsToScroll(FrameworkElement target)
	{
		return (double)target.GetValue(PointsToScrollProperty);
	}

	private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
	{
		if (target is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
		}
	}

	public static void SetIsEnabled(FrameworkElement target, bool value)
	{
		target.SetValue(IsEnabledProperty, value);
	}

	public static bool GetIsEnabled(FrameworkElement target)
	{
		return (bool)target.GetValue(IsEnabledProperty);
	}

	private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender != null && sender is ScrollViewer)
		{
			(sender as ScrollViewer).Loaded += scrollerLoaded;
		}
	}

	private static void AnimateScroll(ScrollViewer scrollViewer, double ToValue)
	{
		DoubleAnimation doubleAnimation = new DoubleAnimation();
		doubleAnimation.From = scrollViewer.VerticalOffset;
		doubleAnimation.To = ToValue;
		doubleAnimation.Duration = new Duration(GetTimeDuration(scrollViewer));
		Storyboard obj = new Storyboard
		{
			Children = { (Timeline)doubleAnimation }
		};
		Storyboard.SetTarget(doubleAnimation, scrollViewer);
		Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath(VerticalOffsetProperty));
		obj.Begin();
	}

	private static double NormalizeScrollPos(ScrollViewer scroll, double scrollChange, Orientation o)
	{
		double result = scrollChange;
		if (scrollChange < 0.0)
		{
			result = 0.0;
		}
		if (o == Orientation.Vertical && scrollChange > scroll.ScrollableHeight)
		{
			result = scroll.ScrollableHeight;
		}
		else if (o == Orientation.Horizontal && scrollChange > scroll.ScrollableWidth)
		{
			result = scroll.ScrollableWidth;
		}
		return result;
	}

	private static void UpdateScrollPosition(object sender)
	{
		if (!(sender is ListBox listBox))
		{
			return;
		}
		double num = 0.0;
		for (int i = 0; i < listBox.SelectedIndex; i++)
		{
			if (listBox.ItemContainerGenerator.ContainerFromItem(listBox.Items[i]) is ListBoxItem listBoxItem)
			{
				num += listBoxItem.ActualHeight;
			}
		}
		AnimateScroll(_listBoxScroller, num);
	}

	private static void SetEventHandlersForScrollViewer(ScrollViewer scroller)
	{
		scroller.PreviewMouseWheel += ScrollViewerPreviewMouseWheel;
		scroller.PreviewKeyDown += ScrollViewerPreviewKeyDown;
	}

	private static void scrollerLoaded(object sender, RoutedEventArgs e)
	{
		SetEventHandlersForScrollViewer(sender as ScrollViewer);
	}

	private static void ScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		double num = e.Delta;
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		double num2 = GetVerticalOffset(scrollViewer) - num / 3.0;
		if (num2 < 0.0)
		{
			AnimateScroll(scrollViewer, 0.0);
		}
		else if (num2 > scrollViewer.ScrollableHeight)
		{
			AnimateScroll(scrollViewer, scrollViewer.ScrollableHeight);
		}
		else
		{
			AnimateScroll(scrollViewer, num2);
		}
		e.Handled = true;
	}

	private static void ScrollViewerPreviewKeyDown(object sender, KeyEventArgs e)
	{
		ScrollViewer scrollViewer = (ScrollViewer)sender;
		Key key = e.Key;
		double num = GetVerticalOffset(scrollViewer);
		bool handled = false;
		switch (key)
		{
		case Key.Down:
			num = NormalizeScrollPos(scrollViewer, num + GetPointsToScroll(scrollViewer), Orientation.Vertical);
			handled = true;
			break;
		case Key.Next:
			num = NormalizeScrollPos(scrollViewer, num + scrollViewer.ViewportHeight, Orientation.Vertical);
			handled = true;
			break;
		case Key.Up:
			num = NormalizeScrollPos(scrollViewer, num - GetPointsToScroll(scrollViewer), Orientation.Vertical);
			handled = true;
			break;
		case Key.Prior:
			num = NormalizeScrollPos(scrollViewer, num - scrollViewer.ViewportHeight, Orientation.Vertical);
			handled = true;
			break;
		}
		if (num != GetVerticalOffset(scrollViewer))
		{
			AnimateScroll(scrollViewer, num);
		}
		e.Handled = handled;
	}

	private static void ListBoxLayoutUpdated(object sender, EventArgs e)
	{
		UpdateScrollPosition(sender);
	}

	private static void ListBoxLoaded(object sender, RoutedEventArgs e)
	{
		UpdateScrollPosition(sender);
	}

	private static void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		UpdateScrollPosition(sender);
	}
}
