using System;
using System.Runtime.InteropServices;

namespace ticktick_WPF.Util;

public static class MemoryUtils
{
	public struct MEMORY_INFO
	{
		public uint dwLength;

		public uint dwMemoryLoad;

		public ulong ullTotalPhys;

		public ulong ullAvailPhys;

		public ulong ullTotalPageFile;

		public ulong ullAvailPageFile;

		public ulong ullTotalVirtual;

		public ulong ullAvailVirtual;

		public ulong ullAvailExtendedVirtual;
	}

	[DllImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GlobalMemoryStatusEx(ref MEMORY_INFO mi);

	public static string FormatSize(double size)
	{
		double num = size;
		int num2 = 0;
		while (num > 1024.0 && num2 < 5)
		{
			num /= 1024.0;
			num2++;
		}
		string[] array = new string[5] { "B", "KB", "MB", "GB", "TB" };
		return $"{Math.Round(num, 2)} {array[num2]}";
	}

	public static MEMORY_INFO GetMemoryStatus()
	{
		MEMORY_INFO mi = default(MEMORY_INFO);
		mi.dwLength = (uint)Marshal.SizeOf(mi);
		GlobalMemoryStatusEx(ref mi);
		return mi;
	}

	public static ulong GetAvailPhys()
	{
		return GetMemoryStatus().ullAvailPhys;
	}

	public static ulong GetUsedPhys()
	{
		MEMORY_INFO memoryStatus = GetMemoryStatus();
		return memoryStatus.ullTotalPhys - memoryStatus.ullAvailPhys;
	}

	public static ulong GetTotalPhys()
	{
		return GetMemoryStatus().ullTotalPhys;
	}
}
