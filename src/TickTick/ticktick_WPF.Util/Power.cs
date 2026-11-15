using System;
using System.Runtime.InteropServices;

namespace ticktick_WPF.Util;

public class Power
{
	public enum PROCESS_INFORMATION_CLASS
	{
		ProcessMemoryPriority,
		ProcessMemoryExhaustionInfo,
		ProcessAppMemoryInfo,
		ProcessInPrivateInfo,
		ProcessPowerThrottling,
		ProcessReservedValue1,
		ProcessTelemetryCoverageInfo,
		ProcessProtectionLevelInfo,
		ProcessLeapSecondInfo,
		ProcessInformationClassMax
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetProcessInformation([In] IntPtr hProcess, [In] PROCESS_INFORMATION_CLASS ProcessInformationClass, IntPtr ProcessInformation, uint ProcessInformationSize);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetPriorityClass(IntPtr handle, uint priorityClass);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

	public static bool SetProcessEcoQoS(IntPtr hProcess, bool bFlag)
	{
		uint val = 1u;
		uint val2 = (bFlag ? 1u : 0u);
		uint val3 = 1u;
		int num = 12;
		IntPtr intPtr = Marshal.AllocHGlobal(num);
		Marshal.WriteInt32(intPtr, (int)val3);
		Marshal.WriteInt32(intPtr + 4, (int)val);
		Marshal.WriteInt32(intPtr + 8, (int)val2);
		bool result = false;
		if (SetProcessInformation(hProcess, PROCESS_INFORMATION_CLASS.ProcessPowerThrottling, intPtr, (uint)num) && SetPriorityClass(hProcess, bFlag ? 64u : 32u))
		{
			result = true;
		}
		Marshal.FreeHGlobal(intPtr);
		return result;
	}
}
