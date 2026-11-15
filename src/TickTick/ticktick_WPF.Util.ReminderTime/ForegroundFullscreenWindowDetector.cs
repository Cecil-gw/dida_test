using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util.ReminderTime;

internal static class ForegroundFullscreenWindowDetector
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public int dwFlags;
	}

	private const int MonitorDefaultToNearest = 2;

	private const int BoundsTolerance = 2;

	public static bool IsOtherApplicationFullscreen()
	{
		try
		{
			IntPtr foregroundWindow = GetForegroundWindow();
			if (foregroundWindow == IntPtr.Zero || foregroundWindow == GetDesktopWindow() || foregroundWindow == GetShellWindow() || !IsWindowVisible(foregroundWindow) || IsIconic(foregroundWindow))
			{
				return false;
			}
			GetWindowThreadProcessId(foregroundWindow, out var processId);
			if (processId == (uint)Process.GetCurrentProcess().Id)
			{
				return false;
			}
			if (!GetWindowRect(foregroundWindow, out var rect))
			{
				return false;
			}
			IntPtr intPtr = MonitorFromWindow(foregroundWindow, 2);
			if (intPtr == IntPtr.Zero)
			{
				return false;
			}
			MONITORINFO monitorInfo = new MONITORINFO
			{
				cbSize = Marshal.SizeOf(typeof(MONITORINFO))
			};
			if (!GetMonitorInfo(intPtr, ref monitorInfo))
			{
				return false;
			}
			RECT rcMonitor = monitorInfo.rcMonitor;
			return rect.Left <= rcMonitor.Left + 2 && rect.Top <= rcMonitor.Top + 2 && rect.Right >= rcMonitor.Right - 2 && rect.Bottom >= rcMonitor.Bottom - 2;
		}
		catch (Exception ex)
		{
			UtilLog.Warn("IsOtherApplicationFullscreen failed: " + ex.Message);
			return false;
		}
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindowVisible(IntPtr hWnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsIconic(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr GetDesktopWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr GetShellWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO monitorInfo);
}
