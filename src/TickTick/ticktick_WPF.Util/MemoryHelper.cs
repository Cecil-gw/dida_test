using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using TickTickUtils;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util;

public static class MemoryHelper
{
	private static Timer _timer = new Timer();

	[DllImport("kernel32.dll")]
	private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

	public static void FlushMemory()
	{
		try
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, 104857600, 419430400);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Warn("FlushMemory Error :" + ExceptionUtils.BuildExceptionMessage(ex));
		}
	}

	public static void Cracker(int sleepSpan = 14400)
	{
		_timer.Interval = sleepSpan * 1000;
		_timer.Elapsed += delegate
		{
			UtilLog.Info("MemoryHelper Begin FlushMemory");
			FlushMemory();
		};
		_timer.Start();
	}

	public static string GetCurrentSize()
	{
		try
		{
			using Process process = Process.GetCurrentProcess();
			int guiResources = GetGuiResources(process.Handle, 0);
			int guiResources2 = GetGuiResources(process.Handle, 2);
			int guiResources3 = GetGuiResources(process.Handle, 1);
			int guiResources4 = GetGuiResources(process.Handle, 4);
			string text = MemoryUtils.FormatSize(process.PrivateMemorySize64);
			string text2 = MemoryUtils.FormatSize(MemoryUtils.GetAvailPhys());
			return $"current:{text},available:{text2},gui:{guiResources}/{guiResources2},userObject:{guiResources3}/{guiResources4},handleCount:{process.HandleCount}";
		}
		catch (Exception ex)
		{
			return "," + ex.Message;
		}
	}

	public static string GetSize()
	{
		try
		{
			using Process process = Process.GetCurrentProcess();
			return MemoryUtils.FormatSize(process.PrivateMemorySize64);
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	[DllImport("User32")]
	private static extern int GetGuiResources(IntPtr hProcess, int uiFlags);
}
