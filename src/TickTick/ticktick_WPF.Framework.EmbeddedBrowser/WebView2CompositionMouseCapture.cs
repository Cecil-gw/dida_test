using System;
using System.Windows;
using System.Windows.Input;

namespace ticktick_WPF.Framework.EmbeddedBrowser;

internal sealed class WebView2CompositionMouseCapture : IDisposable
{
	private readonly FrameworkElement _view;

	private readonly Func<bool> _isReady;

	private readonly Action _cancelDrag;

	private bool _leftButtonDown;

	internal WebView2CompositionMouseCapture(FrameworkElement view, Func<bool> isReady, Action cancelDrag)
	{
		_view = view;
		_isReady = isReady;
		_cancelDrag = cancelDrag;
		_view.AddHandler(Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseDown), handledEventsToo: true);
		_view.AddHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseUp), handledEventsToo: true);
		_view.AddHandler(Mouse.PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMove), handledEventsToo: true);
		_view.LostMouseCapture += OnLostMouseCapture;
		_view.Unloaded += OnUnloaded;
	}

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && e.StylusDevice == null && _isReady())
		{
			_leftButtonDown = true;
			_view.CaptureMouse();
		}
	}

	private void OnMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && e.StylusDevice == null && _leftButtonDown)
		{
			_leftButtonDown = false;
			ReleaseCapture();
		}
	}

	private void OnPreviewMouseMove(object sender, MouseEventArgs e)
	{
		if (_leftButtonDown && e.StylusDevice == null && e.LeftButton == MouseButtonState.Released)
		{
			CancelDrag();
		}
	}

	private void OnLostMouseCapture(object sender, MouseEventArgs e)
	{
		if (!_view.IsMouseCaptured)
		{
			CancelDrag();
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		CancelDrag();
	}

	private void CancelDrag()
	{
		if (!_leftButtonDown)
		{
			return;
		}
		_leftButtonDown = false;
		try
		{
			if (_isReady())
			{
				_cancelDrag();
			}
		}
		finally
		{
			ReleaseCapture();
		}
	}

	private void ReleaseCapture()
	{
		if (_view.IsMouseCaptured)
		{
			_view.ReleaseMouseCapture();
		}
	}

	public void Dispose()
	{
		CancelDrag();
		_view.RemoveHandler(Mouse.MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
		_view.RemoveHandler(Mouse.MouseUpEvent, new MouseButtonEventHandler(OnMouseUp));
		_view.RemoveHandler(Mouse.PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMove));
		_view.LostMouseCapture -= OnLostMouseCapture;
		_view.Unloaded -= OnUnloaded;
	}
}
