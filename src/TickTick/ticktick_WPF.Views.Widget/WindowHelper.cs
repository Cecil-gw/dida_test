using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Widget;

public static class WindowHelper
{
	private enum MONITOR_DEFAULTTO : uint
	{
		MONITOR_DEFAULTTONULL,
		MONITOR_DEFAULTTOPRIMARY,
		MONITOR_DEFAULTTONEAREST
	}

	private struct MONITORINFO
	{
		public uint cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	private struct WINDOWPLACEMENT
	{
		public uint length;

		public uint flags;

		public uint showCmd;

		public POINT ptMinPosition;

		public POINT ptMaxPosition;

		public RECT rcNormalPosition;
	}

	private struct POINT
	{
		public int x;

		public int y;
	}

	private struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;

		public int Width => right - left;

		public int Height => bottom - top;

		public RECT(int x, int y, int width, int height)
		{
			left = x;
			top = y;
			right = x + width;
			bottom = y + height;
		}
	}

	public static void MoveToCenter(Window window)
	{
		if (!GetCursorPos(out var lpPoint))
		{
			return;
		}
		IntPtr hMonitor = MonitorFromPoint(lpPoint, MONITOR_DEFAULTTO.MONITOR_DEFAULTTONULL);
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = (uint)Marshal.SizeOf<MONITORINFO>()
		};
		if (!GetMonitorInfo(hMonitor, ref lpmi) || !GetWindowPlacement(new WindowInteropHelper(window).EnsureHandle(), out var lpwndpl))
		{
			return;
		}
		int num = lpmi.rcWork.left + Math.Max(0, (int)((double)(lpmi.rcWork.Width - lpwndpl.rcNormalPosition.Width) / 2.0));
		int num2 = lpmi.rcWork.top + Math.Max(0, (int)((double)(lpmi.rcWork.Height - lpwndpl.rcNormalPosition.Height) / 2.0));
		if (!Utils.IsWindows7())
		{
			Screen[] allScreens = Screen.AllScreens;
			foreach (Screen screen in allScreens)
			{
				if (screen.WorkingArea.Left == lpmi.rcWork.left && screen.WorkingArea.Top == lpmi.rcWork.top)
				{
					screen.GetDpi(DpiType.Effective, out var dpiX, out var dpiY);
					double num3 = (double)screen.WorkingArea.Left / ((double)dpiX / 96.0);
					double num4 = (double)screen.WorkingArea.Top / ((double)dpiY / 96.0);
					double num5 = (double)screen.WorkingArea.Width / ((double)dpiX / 96.0);
					double num6 = (double)screen.WorkingArea.Height / ((double)dpiY / 96.0);
					num = (int)(num3 + Math.Max(0.0, (num5 - window.ActualWidth) / 2.0));
					num2 = (int)(num4 + Math.Max(0.0, (num6 - window.ActualHeight) / 2.0));
				}
			}
		}
		window.Left = num;
		window.Top = num2;
		try
		{
			window.Activate();
		}
		catch (Exception)
		{
		}
	}

	public static Rectangle GetPointScreenRect(int left, int top)
	{
		IntPtr hMonitor = MonitorFromPoint(new POINT
		{
			x = left,
			y = top
		}, MONITOR_DEFAULTTO.MONITOR_DEFAULTTONEAREST);
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = (uint)Marshal.SizeOf<MONITORINFO>()
		};
		if (GetMonitorInfo(hMonitor, ref lpmi))
		{
			return new Rectangle(lpmi.rcWork.left, lpmi.rcWork.top, lpmi.rcWork.Width, lpmi.rcWork.Height);
		}
		return default(Rectangle);
	}

	public static void MoveTo(Window window, int left, int top)
	{
		if (!((double)left > SystemParameters.PrimaryScreenWidth) && left >= 0 && top >= 0 && !((double)top > SystemParameters.PrimaryScreenHeight))
		{
			return;
		}
		IntPtr hMonitor = MonitorFromPoint(new POINT
		{
			x = left,
			y = top
		}, MONITOR_DEFAULTTO.MONITOR_DEFAULTTONEAREST);
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = (uint)Marshal.SizeOf<MONITORINFO>()
		};
		if (!GetMonitorInfo(hMonitor, ref lpmi) || !GetWindowPlacement(new WindowInteropHelper(window).EnsureHandle(), out var lpwndpl))
		{
			return;
		}
		left = lpmi.rcWork.left + Math.Max(0, (int)((double)(lpmi.rcWork.Width - lpwndpl.rcNormalPosition.Width) / 2.0));
		top = lpmi.rcWork.top + Math.Max(0, (int)((double)(lpmi.rcWork.Height - lpwndpl.rcNormalPosition.Height) / 2.0));
		if (!Utils.IsWindows7())
		{
			Screen[] allScreens = Screen.AllScreens;
			foreach (Screen screen in allScreens)
			{
				if (screen.WorkingArea.Left == lpmi.rcWork.left && screen.WorkingArea.Top == lpmi.rcWork.top)
				{
					screen.GetDpi(DpiType.Effective, out var dpiX, out var dpiY);
					double num = (double)screen.WorkingArea.Left / ((double)dpiX / 96.0);
					double num2 = (double)screen.WorkingArea.Top / ((double)dpiY / 96.0);
					double num3 = (double)screen.WorkingArea.Width / ((double)dpiX / 96.0);
					double num4 = (double)screen.WorkingArea.Height / ((double)dpiY / 96.0);
					left = (int)(num + Math.Max(0.0, (num3 - window.ActualWidth) / 2.0));
					top = (int)(num2 + Math.Max(0.0, (num4 - window.ActualHeight) / 2.0));
				}
			}
		}
		window.Left = left;
		window.Top = top;
		try
		{
			window.Activate();
		}
		catch (Exception)
		{
		}
	}

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("User32.dll")]
	private static extern IntPtr MonitorFromPoint(POINT pt, MONITOR_DEFAULTTO dwFlags);

	[DllImport("User32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

	[DllImport("User32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);
}
