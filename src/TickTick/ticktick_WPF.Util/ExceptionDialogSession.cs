using System;
using System.Windows;
using System.Windows.Threading;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util;

internal sealed class ExceptionDialogSession
{
	private readonly Func<Window> _createWpf;

	private readonly Action<Window> _closeWpf;

	private readonly Action _showNative;

	private Window _wpfWindow;

	private bool _nativeRequested;

	private bool _nativeShown;

	private bool _finished;

	private bool _wpfClosed;

	private ShutdownMode? _previousShutdownMode;

	public ExceptionDialogSession(Exception exception, Func<Window> createWpf, Action<Window> closeWpf, Action showNative)
	{
		_nativeRequested = RenderFailureRecovery.IsRenderFailure(exception);
		_createWpf = createWpf;
		_closeWpf = closeWpf;
		_showNative = showNative;
	}

	public bool TryRequestNative(Exception exception)
	{
		if (_finished || _nativeRequested || _nativeShown || !RenderFailureRecovery.IsRenderFailure(exception))
		{
			return false;
		}
		_nativeRequested = true;
		UtilLog.Error("ExceptionDialog switching to native after render failure: " + exception);
		if (_wpfWindow != null)
		{
			_wpfWindow.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(CloseWpfForFallback));
		}
		return true;
	}

	private void CloseWpfForFallback()
	{
		if (_wpfWindow != null && !_wpfClosed)
		{
			KeepApplicationAlive();
			_closeWpf(_wpfWindow);
		}
	}

	private void KeepApplicationAlive()
	{
		Application current = Application.Current;
		if (current != null && !_previousShutdownMode.HasValue)
		{
			_previousShutdownMode = current.ShutdownMode;
			current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
		}
	}

	public void Show()
	{
		if (_finished || _nativeShown)
		{
			return;
		}
		try
		{
			if (!_nativeRequested)
			{
				try
				{
					_wpfWindow = _createWpf();
					_wpfWindow.Closed += delegate
					{
						_wpfClosed = true;
					};
					if (!_nativeRequested)
					{
						_wpfWindow.ShowDialog();
					}
				}
				catch (Exception exception) when (RenderFailureRecovery.IsRenderFailure(exception))
				{
					TryRequestNative(exception);
				}
			}
			if (_nativeRequested)
			{
				KeepApplicationAlive();
				CloseWpfForFallback();
				_wpfWindow = null;
				_nativeShown = true;
				UtilLog.Info("ExceptionDialog WPF modal loop released; showing native dialog");
				_showNative();
			}
		}
		finally
		{
			_finished = true;
			_wpfWindow = null;
			Application current = Application.Current;
			if (_previousShutdownMode.HasValue && current != null && !current.Dispatcher.HasShutdownStarted)
			{
				try
				{
					current.ShutdownMode = _previousShutdownMode.Value;
				}
				catch (InvalidOperationException)
				{
					UtilLog.Info("ExceptionDialog shutdown mode not restored: application is shutting down");
				}
			}
		}
	}
}
