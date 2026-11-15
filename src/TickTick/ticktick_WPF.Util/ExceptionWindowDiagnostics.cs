using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using TickTickUtils.Lang;

namespace ticktick_WPF.Util;

internal static class ExceptionWindowDiagnostics
{
	private static string Describe(IntPtr handle)
	{
		if (handle == IntPtr.Zero)
		{
			return "0";
		}
		Rectangle rect = NativeUtils.GetWindowRect(handle);
		bool flag = Screen.AllScreens.Any((Screen s) => s.WorkingArea.IntersectsWith(rect));
		return $"0x{handle.ToInt64():X}/valid={IsWindow(handle)}/enabled={IsWindowEnabled(handle)}" + $"/visible={IsWindowVisible(handle)}/min={IsIconic(handle)}/rect={rect}" + $"/onScreen={flag}/owner=0x{NativeUtils.GetWindowOwner(handle).ToInt64():X}";
	}

	public static void LogRenderFailure(Window mainWindow)
	{
		try
		{
			IntPtr handle = ((mainWindow == null) ? IntPtr.Zero : new WindowInteropHelper(mainWindow).Handle);
			UtilLog.Info("RenderRecovery failure main=" + Describe(handle) + ", screens=" + string.Join(";", Screen.AllScreens.Select((Screen s) => s.WorkingArea)));
		}
		catch (Exception)
		{
		}
	}

	[DllImport("user32.dll")]
	private static extern bool IsWindow(IntPtr handle);

	[DllImport("user32.dll")]
	private static extern bool IsWindowEnabled(IntPtr handle);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(IntPtr handle);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr handle);

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();
}
