using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ticktick_WPF.Util.DragAndDrop;

public class DragAutoScrollHelper
{
	private readonly ScrollViewer _scrollViewer;

	private bool _autoScrolling;

	private double _autoScrollOffset;

	private bool _horizonScroll;

	public DragAutoScrollHelper(ListView projectList)
	{
		if (projectList.Template.FindName("ScrollViewer", projectList) is ScrollViewer scrollViewer)
		{
			_scrollViewer = scrollViewer;
		}
	}

	public DragAutoScrollHelper(ScrollViewer scrollViewer)
	{
		_scrollViewer = scrollViewer;
	}

	public void SetAutoScrolling(bool autoScroll)
	{
		_autoScrolling = autoScroll;
	}

	private void SetScrollOffset(double offset)
	{
		_autoScrollOffset = offset;
	}

	private async Task AutoScroll(bool autoScroll)
	{
		if (_autoScrolling || _scrollViewer == null)
		{
			return;
		}
		_autoScrolling = true;
		await Task.Delay(180);
		while (_autoScrolling)
		{
			if (_horizonScroll)
			{
				_scrollViewer.ScrollToHorizontalOffset(_scrollViewer.HorizontalOffset + (double)((!autoScroll) ? 1 : (-1)) * Math.Max(6.0, _autoScrollOffset));
			}
			else
			{
				_scrollViewer.ScrollToVerticalOffset(_scrollViewer.VerticalOffset + (double)((!autoScroll) ? 1 : (-1)) * Math.Max(6.0, _autoScrollOffset));
			}
			await Task.Delay(30);
		}
	}

	public bool TryAutoScroll(MouseEventArgs e, bool checkHorizon = false, double leftMargin = 0.0, double rightMargin = 0.0, double scrollerWidthDiff = 0.0, double horizontalScrollTopOffset = 0.0)
	{
		if (_scrollViewer == null)
		{
			return false;
		}
		Point position = e.GetPosition(_scrollViewer);
		if (checkHorizon && position.Y > horizontalScrollTopOffset && position.Y < _scrollViewer.ActualHeight)
		{
			bool? flag = null;
			double x = position.X;
			double num = _scrollViewer.ActualWidth - scrollerWidthDiff;
			if (x > 0.0 && x < 40.0 + leftMargin && _scrollViewer.HorizontalOffset > 4.0)
			{
				SetScrollOffset(Math.Min(40.0, 40.0 + leftMargin - x) * 1.5);
				flag = true;
			}
			else if (x < num && x > num - 40.0 - rightMargin && _scrollViewer.HorizontalOffset < _scrollViewer.ScrollableWidth - 4.0)
			{
				SetScrollOffset(Math.Min(40.0, 40.0 + rightMargin + x - num) * 1.5);
				flag = false;
			}
			if (flag.HasValue)
			{
				_horizonScroll = true;
				AutoScroll(flag.Value);
				return true;
			}
		}
		if (position.X > leftMargin && position.X < _scrollViewer.ActualWidth - rightMargin)
		{
			bool? flag2 = null;
			if (position.Y > 0.0 && position.Y < 40.0 && _scrollViewer.VerticalOffset > 4.0)
			{
				SetScrollOffset((40.0 - position.Y) * 1.5);
				flag2 = true;
			}
			else if (position.Y < _scrollViewer.ActualHeight && position.Y > _scrollViewer.ActualHeight - 40.0 && _scrollViewer.VerticalOffset < _scrollViewer.ScrollableHeight - 4.0)
			{
				SetScrollOffset((40.0 + position.Y - _scrollViewer.ActualHeight) * 1.5);
				flag2 = false;
			}
			if (flag2.HasValue)
			{
				_horizonScroll = false;
				AutoScroll(flag2.Value);
				return true;
			}
		}
		SetAutoScrolling(autoScroll: false);
		return false;
	}
}
