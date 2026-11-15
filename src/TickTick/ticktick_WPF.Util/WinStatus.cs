using System;

namespace ticktick_WPF.Util;

public static class WinStatus
{
	public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

	public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

	public static readonly IntPtr HWND_TOP = new IntPtr(0);

	public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

	public const uint SWP_NOSIZE = 1u;

	public const uint SWP_NOMOVE = 2u;

	public const uint SWP_NOZORDER = 4u;

	public const uint SWP_NOREDRAW = 8u;

	public const uint SWP_NOACTIVATE = 16u;

	public const uint SWP_FRAMECHANGED = 32u;

	public const uint SWP_SHOWWINDOW = 64u;

	public const uint SWP_HIDEWINDOW = 128u;

	public const uint SWP_NOCOPYBITS = 256u;

	public const uint SWP_NOOWNERZORDER = 512u;

	public const uint SWP_NOSENDCHANGING = 1024u;

	public const uint TOPMOST_FLAGS = 3u;

	public const uint ZPOS_FLAGS = 1555u;

	public const uint WM_ACTIVATEAPP = 28u;

	public const uint WM_ACTIVATE = 6u;

	public const uint WM_SETFOCUS = 7u;

	public const uint WM_NCCALCSIZE = 131u;

	public const uint WM_WINDOWPOSCHANGING = 70u;
}
