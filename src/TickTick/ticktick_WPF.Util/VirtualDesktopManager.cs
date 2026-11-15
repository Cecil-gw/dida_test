using System;
using System.Runtime.InteropServices;

namespace ticktick_WPF.Util;

public class VirtualDesktopManager
{
	private static readonly VirtualDesktopManager Vdm;

	private CVirtualDesktopManager cmanager;

	private IVirtualDesktopManager manager;

	static VirtualDesktopManager()
	{
		Vdm = new VirtualDesktopManager();
	}

	public static bool IsWindowInCurrentDeskTop(IntPtr window)
	{
		return Vdm.IsWindowOnCurrentVirtualDesktop(window);
	}

	public VirtualDesktopManager()
	{
		cmanager = new CVirtualDesktopManager();
		manager = (IVirtualDesktopManager)cmanager;
	}

	~VirtualDesktopManager()
	{
		manager = null;
		cmanager = null;
	}

	public bool IsWindowOnCurrentVirtualDesktop(IntPtr TopLevelWindow)
	{
		int errorCode;
		if ((errorCode = manager.IsWindowOnCurrentVirtualDesktop(TopLevelWindow, out var OnCurrentDesktop)) != 0)
		{
			Marshal.ThrowExceptionForHR(errorCode);
		}
		return OnCurrentDesktop != 0;
	}

	public Guid GetWindowDesktopId(IntPtr TopLevelWindow)
	{
		int windowDesktopId;
		if ((windowDesktopId = manager.GetWindowDesktopId(TopLevelWindow, out var CurrentDesktop)) != 0)
		{
			Marshal.ThrowExceptionForHR(windowDesktopId);
		}
		return CurrentDesktop;
	}

	public void MoveWindowToDesktop(IntPtr TopLevelWindow, Guid CurrentDesktop)
	{
		int errorCode;
		if ((errorCode = manager.MoveWindowToDesktop(TopLevelWindow, CurrentDesktop)) != 0)
		{
			Marshal.ThrowExceptionForHR(errorCode);
		}
	}
}
