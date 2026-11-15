using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.AI;

internal sealed class AIDesktopWindowState : IDisposable
{
	private const int WmSysCommand = 274;

	private const int ScMaximize = 61488;

	private const int WmStyleChanging = 124;

	private readonly Window _window;

	private HwndSource _source;

	internal AIDesktopWindowState(Window window)
	{
		_window = window;
		_window.SourceInitialized += OnSourceInitialized;
		_window.StateChanged += OnStateChanged;
		_window.Closed += OnClosed;
		OnStateChanged(window, EventArgs.Empty);
		if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
		{
			InitializeSource();
		}
	}

	private void OnSourceInitialized(object sender, EventArgs e)
	{
		InitializeSource();
	}

	private void InitializeSource()
	{
		if (_source == null)
		{
			IntPtr handle = new WindowInteropHelper(_window).Handle;
			_source = HwndSource.FromHwnd(handle);
			_source?.AddHook(WndProc);
			int windowLong = NativeUtils.GetWindowLong(handle, -16);
			NativeUtils.SetWindowLong(handle, WinParameter.GWL_STYLE, windowLong & -65537);
		}
	}

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 274 && (wParam.ToInt64() & 0xFFF0) == 61488)
		{
			handled = true;
		}
		else if (msg == 124 && wParam.ToInt64() == -16 && lParam != IntPtr.Zero)
		{
			int num = Marshal.ReadInt32(lParam, 4);
			Marshal.WriteInt32(lParam, 4, num & -65537);
		}
		return IntPtr.Zero;
	}

	private void OnStateChanged(object sender, EventArgs e)
	{
		if (_window.WindowState == WindowState.Maximized)
		{
			_window.WindowState = WindowState.Normal;
		}
	}

	private void OnClosed(object sender, EventArgs e)
	{
		Dispose();
	}

	public void Dispose()
	{
		_window.SourceInitialized -= OnSourceInitialized;
		_window.StateChanged -= OnStateChanged;
		_window.Closed -= OnClosed;
		_source?.RemoveHook(WndProc);
		_source = null;
	}
}
