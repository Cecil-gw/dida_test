using System;

namespace ticktick_WPF.Util;

public class VirtualDesktopUtils
{
	public static bool GetIsInCurrent(IntPtr handle)
	{
		if (Utils.IsWindows7())
		{
			return true;
		}
		return VirtualDesktopManager.IsWindowInCurrentDeskTop(handle);
	}
}
