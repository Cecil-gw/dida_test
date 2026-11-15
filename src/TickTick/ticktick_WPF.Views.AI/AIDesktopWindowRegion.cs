using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace ticktick_WPF.Views.AI;

internal static class AIDesktopWindowRegion
{
	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	[DllImport("gdi32.dll")]
	private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(IntPtr region);

	[DllImport("user32.dll")]
	private static extern int SetWindowRgn(IntPtr window, IntPtr region, bool redraw);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

	internal static bool NeedsAlignment(Window window, System.Windows.Point ballCenter, System.Windows.Size ballSize)
	{
		IntPtr handle = new WindowInteropHelper(window).Handle;
		if (handle == IntPtr.Zero || !GetWindowRect(handle, out var rect))
		{
			return false;
		}
		Screen screen = Screen.FromPoint(new System.Drawing.Point((int)Math.Round(ballCenter.X), (int)Math.Round(ballCenter.Y)));
		DpiScale dpi = VisualTreeHelper.GetDpi(window);
		double num = ballSize.Width * dpi.DpiScaleX / 2.0;
		double num2 = ballSize.Height * dpi.DpiScaleY / 2.0;
		if (!(Screen.FromHandle(handle).DeviceName != screen.DeviceName) && !(ballCenter.X - num < (double)rect.Left - 0.5) && !(ballCenter.Y - num2 < (double)rect.Top - 0.5) && !(ballCenter.X + num > (double)rect.Right + 0.5))
		{
			return ballCenter.Y + num2 > (double)rect.Bottom + 0.5;
		}
		return true;
	}

	internal static bool AlignToScreen(Window window, System.Windows.Point ballCenter, System.Windows.Size ballSize)
	{
		IntPtr handle = new WindowInteropHelper(window).Handle;
		if (handle == IntPtr.Zero)
		{
			return false;
		}
		Rectangle bounds = Screen.FromPoint(new System.Drawing.Point((int)Math.Round(ballCenter.X), (int)Math.Round(ballCenter.Y))).Bounds;
		bool result = false;
		for (int i = 0; i < 2; i++)
		{
			if (!GetWindowRect(handle, out var rect))
			{
				return result;
			}
			DpiScale dpi = VisualTreeHelper.GetDpi(window);
			double num = ballSize.Width * dpi.DpiScaleX / 2.0;
			double num2 = ballSize.Height * dpi.DpiScaleY / 2.0;
			if (!NeedsAlignment(window, ballCenter, ballSize))
			{
				return result;
			}
			int val = Math.Max(bounds.Left, Math.Min(rect.Left, bounds.Right - (rect.Right - rect.Left)));
			int val2 = Math.Max(bounds.Top, Math.Min(rect.Top, bounds.Bottom - (rect.Bottom - rect.Top)));
			val = Math.Max((int)Math.Ceiling(ballCenter.X + num - (double)(rect.Right - rect.Left)), Math.Min(val, (int)Math.Floor(ballCenter.X - num)));
			val2 = Math.Max((int)Math.Ceiling(ballCenter.Y + num2 - (double)(rect.Bottom - rect.Top)), Math.Min(val2, (int)Math.Floor(ballCenter.Y - num2)));
			if (val == rect.Left && val2 == rect.Top)
			{
				return result;
			}
			if (!SetWindowPos(handle, IntPtr.Zero, val, val2, 0, 0, 533u))
			{
				return result;
			}
			result = true;
		}
		return result;
	}

	internal static bool Apply(Visual window, Rect? bounds)
	{
		HwndSource hwndSource = PresentationSource.FromVisual(window) as HwndSource;
		if (hwndSource?.CompositionTarget == null)
		{
			return false;
		}
		if (!bounds.HasValue)
		{
			return SetWindowRgn(hwndSource.Handle, IntPtr.Zero, redraw: true) != 0;
		}
		Rect rect = (bounds.Value.IsEmpty ? default(Rect) : bounds.Value);
		Matrix transformToDevice = hwndSource.CompositionTarget.TransformToDevice;
		System.Windows.Point point = transformToDevice.Transform(rect.TopLeft);
		System.Windows.Point point2 = transformToDevice.Transform(rect.BottomRight);
		IntPtr intPtr = CreateRectRgn((int)Math.Floor(point.X), (int)Math.Floor(point.Y), (int)Math.Ceiling(point2.X), (int)Math.Ceiling(point2.Y));
		if (intPtr == IntPtr.Zero)
		{
			return false;
		}
		if (SetWindowRgn(hwndSource.Handle, intPtr, redraw: true) != 0)
		{
			return true;
		}
		DeleteObject(intPtr);
		return false;
	}
}
