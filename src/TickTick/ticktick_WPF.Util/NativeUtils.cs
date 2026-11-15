using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using ticktick_WPF.Resource;

namespace ticktick_WPF.Util;

internal static class NativeUtils
{
	[Flags]
	public enum DwmWindowAttribute : uint
	{
		DWMWA_CLOAK = 0xDu,
		DWMWA_USE_IMMERSIVE_DARK_MODE = 0x14u,
		DWMWA_MICA_EFFECT = 0x405u
	}

	public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

	private struct TaskbarAppBarData
	{
		public int cbSize;

		public IntPtr hWnd;

		public uint callbackMessage;

		public uint edge;

		public WindowSizing.RECT rect;

		public IntPtr lParam;
	}

	private struct InteropPoint
	{
		public int X;

		public int Y;
	}

	public const int WS_MAXIMIZEBOX = 65536;

	public const int WS_MINIMIZEBOX = 131072;

	public const long WS_POPUP = 2147483648L;

	public const long WS_VISIBLE = 268435456L;

	public const long WS_CLIPSIBLINGS = 1073741824L;

	public const long WS_CLIPCHILDREN = 536870912L;

	public const long WS_EX_LEFT = 0L;

	public const long WS_EX_LTRREADING = 0L;

	public const long WS_EX_RIGHTSCROLLBAR = 0L;

	public const int WS_EX_TOPMOST = 8;

	public const long WS_EX_TOOLWINDOW = 128L;

	private const int WS_EX_TRANSPARENT = 32;

	private const int GWL_EXSTYLE = -20;

	public const int WM_MOUSEHWHEEL = 526;

	[DllImport("dwmapi.dll")]
	public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern IntPtr FindWindow([MarshalAs(UnmanagedType.LPTStr)] string lpClassName, [MarshalAs(UnmanagedType.LPTStr)] string lpWindowName);

	[DllImport("user32")]
	private static extern IntPtr FindWindowEx(IntPtr hWnd1, IntPtr hWnd2, string lpsz1, string lpsz2);

	[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
	private static extern int SetWindowLong32(IntPtr windowHandle, WinParameter nIndex, int dwNewLong);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
	private static extern IntPtr SetWindowLongPtr64(IntPtr windowHandle, WinParameter nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll")]
	public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll")]
	public static extern long SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);

	[DllImport("user32.dll")]
	public static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, uint fuFlage, uint timeout, IntPtr result);

	[DllImport("user32.dll")]
	public static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

	[DllImport("user32.dll")]
	public static extern IntPtr SetParent(IntPtr hwnd, IntPtr parentHwnd);

	[DllImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool NativeGetWindowRect(IntPtr hWnd, out WindowSizing.RECT lpRect);

	public static int GetWindowLong(IntPtr hwnd)
	{
		return GetWindowLong(hwnd, -8);
	}

	public static Rectangle GetWindowRect(IntPtr hwnd)
	{
		NativeGetWindowRect(hwnd, out var lpRect);
		return Rectangle.FromLTRB(lpRect.left, lpRect.top, lpRect.right, lpRect.bottom);
	}

	[DllImport("shell32.dll", EntryPoint = "SHAppBarMessage")]
	private static extern IntPtr GetAppBarMessage(uint message, ref TaskbarAppBarData data);

	internal static bool IsTaskbarAutoHideEnabled()
	{
		TaskbarAppBarData data = new TaskbarAppBarData
		{
			cbSize = Marshal.SizeOf(typeof(TaskbarAppBarData))
		};
		return (GetAppBarMessage(4u, ref data).ToInt64() & 1) != 0;
	}

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromPoint(System.Drawing.Point point, uint flags);

	internal static Rectangle GetScreenWorkingArea(Screen screen)
	{
		Rectangle bounds = screen.Bounds;
		IntPtr hMonitor = MonitorFromPoint(new System.Drawing.Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2), 2u);
		WindowSizing.MONITORINFO mONITORINFO = new WindowSizing.MONITORINFO();
		if (!WindowSizing.GetMonitorInfo(hMonitor, mONITORINFO))
		{
			return screen.WorkingArea;
		}
		WindowSizing.RECT rcWork = mONITORINFO.rcWork;
		return Rectangle.FromLTRB(rcWork.left, rcWork.top, rcWork.right, rcWork.bottom);
	}

	public static int SetWindowLong(IntPtr windowHandle, WinParameter nIndex, int dwNewLong)
	{
		if (IntPtr.Size == 8)
		{
			return (int)SetWindowLongPtr64(windowHandle, nIndex, new IntPtr(dwNewLong));
		}
		return SetWindowLong32(windowHandle, nIndex, dwNewLong);
	}

	public static void RestoreTopmost(Window window)
	{
		IntPtr handle = new WindowInteropHelper(window).Handle;
		if (!(handle == IntPtr.Zero) && (GetWindowLong(handle, -20) & 8) == 0)
		{
			SetWindowPos(handle, WinStatus.HWND_TOPMOST, 0, 0, 0, 0, 19u);
		}
	}

	public static IntPtr GetDesktopPtr()
	{
		IntPtr intPtr = IntPtr.Zero;
		_ = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		IntPtr intPtr3 = FindWindow("Progman", "Program Manager");
		if (intPtr3 != IntPtr.Zero)
		{
			IntPtr intPtr4 = FindWindowEx(intPtr3, IntPtr.Zero, "SHELLDLL_DefView", null);
			if (intPtr4 != IntPtr.Zero)
			{
				intPtr2 = FindWindowEx(intPtr4, IntPtr.Zero, "SysListView32", null);
			}
		}
		if (intPtr2 != IntPtr.Zero)
		{
			return intPtr2;
		}
		while (intPtr2 == IntPtr.Zero)
		{
			intPtr = FindWindowEx(IntPtr.Zero, intPtr, "WorkerW", null);
			if (intPtr == IntPtr.Zero)
			{
				break;
			}
			IntPtr intPtr5 = FindWindowEx(intPtr, IntPtr.Zero, "SHELLDLL_DefView", null);
			if (!(intPtr5 == IntPtr.Zero))
			{
				intPtr2 = FindWindowEx(intPtr5, IntPtr.Zero, "SysListView32", null);
			}
		}
		return intPtr2;
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int count);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr hwnd);

	public static IntPtr GetWindowOwner(IntPtr hwnd)
	{
		return GetWindow(hwnd, 4u);
	}

	[DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
	private static extern int SetOwner32(IntPtr hwnd, int index, int owner);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static extern IntPtr SetOwner64(IntPtr hwnd, int index, IntPtr owner);

	[DllImport("kernel32.dll", EntryPoint = "SetLastError")]
	private static extern void ClearLastError(uint error);

	public static int SetWindowOwner(IntPtr hwnd, IntPtr owner)
	{
		ClearLastError(0u);
		if (!(((IntPtr.Size == 8) ? SetOwner64(hwnd, -8, owner) : new IntPtr(SetOwner32(hwnd, -8, owner.ToInt32()))) == IntPtr.Zero))
		{
			return 0;
		}
		return Marshal.GetLastWin32Error();
	}

	public static string DescribeWidgetDesktop(IntPtr hwnd)
	{
		int visible = 0;
		int minimized = 0;
		EnumWindows(delegate(IntPtr window, IntPtr _)
		{
			if (IsWindowVisible(window))
			{
				visible++;
				if (IsIconic(window))
				{
					minimized++;
				}
			}
			return true;
		}, IntPtr.Zero);
		return "window=" + Describe(hwnd) + ", owner=" + Describe(GetWindowOwner(hwnd)) + ", desktop=" + Describe(GetDesktopPtr()) + ", foreground=" + Describe(GetForegroundWindow()) + ", " + $"style=0x{GetWindowLong(hwnd, -16):X}, exStyle=0x{GetWindowLong(hwnd, -20):X}, " + $"visibleWindows={visible}, minimizedWindows={minimized}";
		static string Describe(IntPtr window)
		{
			if (window == IntPtr.Zero)
			{
				return "0";
			}
			StringBuilder stringBuilder = new StringBuilder(256);
			GetClassName(window, stringBuilder, stringBuilder.Capacity);
			return $"0x{window.ToInt64():X}/{stringBuilder}/visible={IsWindowVisible(window)}/min={IsIconic(window)}/rect={GetWindowRect(window)}";
		}
	}

	public static IntPtr SendMsgToProgman()
	{
		IntPtr intPtr = FindWindow("Progman", null);
		SendMessageTimeout(result: IntPtr.Zero, hwnd: intPtr, msg: 1324u, wParam: IntPtr.Zero, lParam: IntPtr.Zero, fuFlage: 0u, timeout: 2u);
		EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
		{
			if (FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
			{
				ShowWindow(FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null), 0);
			}
			return true;
		}, IntPtr.Zero);
		return intPtr;
	}

	[DllImport("user32.dll")]
	private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

	public static void EnableBlur(bool enable, Window window)
	{
		try
		{
			if (!Utils.IsWindows7() && !Utils.IsWindows11())
			{
				WindowInteropHelper windowInteropHelper = new WindowInteropHelper(window);
				AccentPolicy structure = new AccentPolicy
				{
					AccentState = (enable ? AccentState.ACCENT_ENABLE_BLURBEHIND : AccentState.ACCENT_DISABLED)
				};
				int num = Marshal.SizeOf(structure);
				IntPtr intPtr = Marshal.AllocHGlobal(num);
				Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
				WindowCompositionAttributeData data = new WindowCompositionAttributeData
				{
					Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
					SizeOfData = num,
					Data = intPtr
				};
				SetWindowCompositionAttribute(windowInteropHelper.Handle, ref data);
				Marshal.FreeHGlobal(intPtr);
			}
		}
		catch (Exception)
		{
		}
	}

	public static void SetDarkTheme(HwndSource source, bool darkThemeEnabled)
	{
		int pvAttribute = 1;
		int pvAttribute2 = 0;
		if (darkThemeEnabled)
		{
			DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref pvAttribute, Marshal.SizeOf(typeof(int)));
		}
		else
		{
			DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref pvAttribute2, Marshal.SizeOf(typeof(int)));
		}
		DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref pvAttribute, Marshal.SizeOf(typeof(int)));
	}

	public static void SetWindowExTransparent(IntPtr hwnd)
	{
		int windowLong = GetWindowLong(hwnd, -20);
		SetWindowLong(hwnd, -20, windowLong | 0x20);
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetCursorPos(ref InteropPoint lpPoint);

	public static System.Windows.Point GetCursorPosition()
	{
		InteropPoint lpPoint = default(InteropPoint);
		GetCursorPos(ref lpPoint);
		double pointDpi = ScreenPositionUtils.GetPointDpi(new System.Windows.Point(lpPoint.X, lpPoint.Y));
		return new System.Windows.Point((double)lpPoint.X / pointDpi, (double)lpPoint.Y / pointDpi);
	}
}
