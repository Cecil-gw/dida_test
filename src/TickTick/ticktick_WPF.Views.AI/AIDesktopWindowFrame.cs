using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ticktick_WPF.Views.AI;

internal static class AIDesktopWindowFrame
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct NativeBlend
	{
		public byte Operation;

		public byte Flags;

		public byte ConstantAlpha;

		public byte AlphaFormat;
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr destinationDc, IntPtr destination, IntPtr size, IntPtr sourceDc, IntPtr source, uint colorKey, ref NativeBlend blend, uint flags);

	internal static bool Clear(Window window)
	{
		IntPtr handle = new WindowInteropHelper(window).Handle;
		if (handle == IntPtr.Zero)
		{
			return false;
		}
		NativeBlend blend = new NativeBlend
		{
			ConstantAlpha = 0,
			AlphaFormat = 1
		};
		return UpdateLayeredWindow(handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0u, ref blend, 2u);
	}

	internal static void Flush()
	{
		DwmFlush();
	}
}
