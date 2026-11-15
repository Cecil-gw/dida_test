using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace ticktick_WPF.Util;

public static class WindowSizing
{
	public enum ABEdge
	{
		ABE_LEFT,
		ABE_TOP,
		ABE_RIGHT,
		ABE_BOTTOM
	}

	[Flags]
	public enum ABState
	{
		ABS_AUTOHIDE = 1
	}

	public enum ABMsg
	{
		ABM_GETSTATE = 4,
		ABM_GETTASKBARPOS
	}

	public struct APPBARDATA
	{
		public int cbSize;

		public IntPtr hWnd;

		public int uCallbackMessage;

		public int uEdge;

		public RECT rc;

		public bool lParam;
	}

	public struct MINMAXINFO
	{
		public POINT ptReserved;

		public POINT ptMaxSize;

		public POINT ptMaxPosition;

		public POINT ptMinTrackSize;

		public POINT ptMaxTrackSize;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class MONITORINFO
	{
		public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

		public RECT rcMonitor;

		public RECT rcWork;

		public int dwFlags;
	}

	public struct POINT
	{
		public int x;

		public int y;

		public POINT(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}

	public struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	private const int MONITOR_DEFAULTTONEAREST = 2;

	private static Dictionary<Window, Action> _disposeHandlers = new Dictionary<Window, Action>();

	[DllImport("shell32", CallingConvention = CallingConvention.StdCall)]
	public static extern int SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

	[DllImport("user32", SetLastError = true)]
	private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32")]
	internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

	[DllImport("user32")]
	internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

	private static MINMAXINFO AdjustWorkingAreaForAutoHide(IntPtr monitorContainingApplication, MINMAXINFO mmi)
	{
		IntPtr intPtr = FindWindow("Shell_TrayWnd", null);
		IntPtr intPtr2 = MonitorFromWindow(intPtr, 2);
		if (!monitorContainingApplication.Equals(intPtr2))
		{
			return mmi;
		}
		APPBARDATA pData = default(APPBARDATA);
		pData.cbSize = Marshal.SizeOf(pData);
		pData.hWnd = intPtr;
		SHAppBarMessage(5, ref pData);
		ABEdge edge = GetEdge(pData.rc);
		ABState aBState = (ABState)SHAppBarMessage(4, ref pData);
		if (aBState.HasFlag(ABState.ABS_AUTOHIDE))
		{
			AdjustSizeForAutohide(edge, ref mmi);
		}
		return mmi;
	}

	private static void AdjustSizeForAutohide(ABEdge uEdge, ref MINMAXINFO mmi)
	{
		switch (uEdge)
		{
		case ABEdge.ABE_LEFT:
			mmi.ptMaxPosition.x += 2;
			mmi.ptMaxTrackSize.x -= 2;
			mmi.ptMaxSize.x -= 2;
			break;
		case ABEdge.ABE_RIGHT:
			mmi.ptMaxSize.x -= 2;
			mmi.ptMaxTrackSize.x -= 2;
			break;
		case ABEdge.ABE_TOP:
			mmi.ptMaxPosition.y += 2;
			mmi.ptMaxTrackSize.y -= 2;
			mmi.ptMaxSize.y -= 2;
			break;
		case ABEdge.ABE_BOTTOM:
			mmi.ptMaxSize.y -= 2;
			mmi.ptMaxTrackSize.y -= 2;
			break;
		}
	}

	private static ABEdge GetEdge(RECT rc)
	{
		if (rc.top == rc.left && rc.bottom > rc.right)
		{
			return ABEdge.ABE_LEFT;
		}
		if (rc.top == rc.left && rc.bottom < rc.right)
		{
			return ABEdge.ABE_TOP;
		}
		if (rc.top > rc.left)
		{
			return ABEdge.ABE_BOTTOM;
		}
		return ABEdge.ABE_RIGHT;
	}

	public static Rectangle GetMonitorFromWindowLocation(Window window)
	{
		return Screen.FromRectangle(new Rectangle((int)window.Left, (int)window.Top, (int)window.Width, (int)window.Height)).WorkingArea;
	}

	public static Rectangle GetMonitorFromWindow(Window window)
	{
		HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(window);
		if (hwndSource != null && VirtualDesktopUtils.GetIsInCurrent(hwndSource.Handle))
		{
			return Screen.FromHandle(hwndSource.Handle).WorkingArea;
		}
		return default(Rectangle);
	}

	public static bool WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam, double minWidth, double minHeight)
	{
		IntPtr intPtr = MonitorFromWindow(hwnd, 2);
		if (intPtr != IntPtr.Zero)
		{
			MONITORINFO mONITORINFO = new MONITORINFO();
			GetMonitorInfo(intPtr, mONITORINFO);
			RECT rcWork = mONITORINFO.rcWork;
			RECT rcMonitor = mONITORINFO.rcMonitor;
			int num = 0;
			MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
			mmi.ptMaxPosition.x = Math.Abs(rcWork.left - rcMonitor.left) - num;
			mmi.ptMaxPosition.y = Math.Abs(rcWork.top - rcMonitor.top) - num;
			mmi.ptMaxSize.x = Math.Abs(rcWork.right - rcWork.left) + 2 * num;
			mmi.ptMaxSize.y = Math.Abs(rcWork.bottom - rcWork.top) + 2 * num;
			mmi.ptMaxTrackSize.x = mmi.ptMaxSize.x;
			mmi.ptMaxTrackSize.y = mmi.ptMaxSize.y;
			double scalingRatio = ScreenPositionUtils.GetScalingRatio();
			mmi.ptMinTrackSize.x = (int)(minWidth * scalingRatio);
			mmi.ptMinTrackSize.y = (int)(minHeight * scalingRatio);
			APPBARDATA pData = default(APPBARDATA);
			pData.cbSize = Marshal.SizeOf(pData);
			IntPtr hWnd = FindWindow("Shell_TrayWnd", null);
			pData.hWnd = hWnd;
			SHAppBarMessage(5, ref pData);
			ABEdge edge = GetEdge(pData.rc);
			ABState aBState = (ABState)SHAppBarMessage(4, ref pData);
			if (aBState.HasFlag(ABState.ABS_AUTOHIDE))
			{
				AdjustSizeForAutohide(edge, ref mmi);
			}
			Marshal.StructureToPtr((object)mmi, lParam, fDeleteOld: true);
			return true;
		}
		return false;
	}
}
