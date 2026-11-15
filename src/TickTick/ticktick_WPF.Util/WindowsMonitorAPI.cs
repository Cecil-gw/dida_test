using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace ticktick_WPF.Util;

public static class WindowsMonitorAPI
{
	public delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
	public class MONITORINFOEX
	{
		internal int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));

		internal RECT rcMonitor;

		internal RECT rcWork;

		internal int dwFlags;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		internal char[] szDevice = new char[32];
	}

	public struct RECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;

		public RECT(Rect r)
		{
			left = (int)r.Left;
			top = (int)r.Top;
			right = (int)r.Right;
			bottom = (int)r.Bottom;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class COMRECT
	{
		public int left;

		public int top;

		public int right;

		public int bottom;
	}

	private const string User32 = "user32.dll";

	public static readonly HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	public static extern bool GetMonitorInfo(HandleRef hmonitor, [In][Out] MONITORINFOEX info);

	[DllImport("user32.dll", ExactSpelling = true)]
	public static extern bool EnumDisplayMonitors(HandleRef hdc, COMRECT rcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
}
