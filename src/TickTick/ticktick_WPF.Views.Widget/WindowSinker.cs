using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ticktick_WPF.Views.Widget;

public class WindowSinker
{
	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOACTIVATE = 16u;

	private const uint SWP_NOZORDER = 4u;

	private const int WM_ACTIVATEAPP = 28;

	private const int WM_ACTIVATE = 6;

	private const int WM_SETFOCUS = 7;

	private const int WM_NCCALCSIZE = 131;

	private const int WM_WINDOWPOSCHANGING = 70;

	private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

	private Window Window;

	public WindowSinker(Window Window)
	{
		this.Window = Window;
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern IntPtr DeferWindowPos(IntPtr hWinPosInfo, IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern IntPtr BeginDeferWindowPos(int nNumWindows);

	[DllImport("user32.dll")]
	private static extern bool EndDeferWindowPos(IntPtr hWinPosInfo);

	private void OnClosing(object sender, CancelEventArgs e)
	{
		HwndSource.FromHwnd(new WindowInteropHelper(Window).Handle).RemoveHook(WndProc);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		SetWindowPos(new WindowInteropHelper(Window).Handle, HWND_BOTTOM, 0, 0, 0, 0, 19u);
		HwndSource.FromHwnd(new WindowInteropHelper(Window).Handle).AddHook(WndProc);
	}

	private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 7 || msg == 131)
		{
			hWnd = new WindowInteropHelper(Window).Handle;
			SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, 19u);
			handled = true;
		}
		return IntPtr.Zero;
	}

	public void Sink()
	{
		Window.Loaded += OnLoaded;
		Window.Closing += OnClosing;
	}

	public void Unsink()
	{
		Window.Loaded -= OnLoaded;
		Window.Closing -= OnClosing;
	}
}
