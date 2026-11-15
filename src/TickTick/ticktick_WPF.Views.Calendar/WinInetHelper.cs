using System;
using System.Runtime.InteropServices;

namespace ticktick_WPF.Views.Calendar;

public static class WinInetHelper
{
	public static bool SupressCookiePersist()
	{
		return SetOption(81, 3);
	}

	public static bool EndBrowserSession()
	{
		return SetOption(42, null);
	}

	private static bool SetOption(int settingCode, int? option)
	{
		IntPtr intPtr = IntPtr.Zero;
		int num = 0;
		if (option.HasValue)
		{
			num = 4;
			intPtr = Marshal.AllocCoTaskMem(num);
			Marshal.WriteInt32(intPtr, option.Value);
		}
		bool result = InternetSetOption(0, settingCode, intPtr, num);
		if (intPtr != IntPtr.Zero)
		{
			Marshal.Release(intPtr);
		}
		return result;
	}

	[DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool InternetSetOption(int hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
}
