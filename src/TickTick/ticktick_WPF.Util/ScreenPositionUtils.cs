using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace ticktick_WPF.Util;

public class ScreenPositionUtils
{
	public static void GetScreens()
	{
		_ = Control.MousePosition;
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen obj in allScreens)
		{
			_ = SystemParameters.VirtualScreenHeight;
			_ = SystemParameters.PrimaryScreenHeight;
			_ = obj.Bounds;
		}
	}

	public static Rectangle GetPointLocationScreen(System.Windows.Point point1, System.Windows.Point? point2)
	{
		Screen[] allScreens = Screen.AllScreens;
		for (int i = 0; i < allScreens.Length; i++)
		{
			Rectangle workingArea = allScreens[i].WorkingArea;
			if (point1.X <= (double)(workingArea.X + workingArea.Width) && point1.X >= (double)workingArea.X && point1.Y <= (double)(workingArea.Y + workingArea.Height) && point1.Y >= (double)workingArea.Y)
			{
				return workingArea;
			}
		}
		if (point2.HasValue)
		{
			System.Windows.Point value = point2.Value;
			allScreens = Screen.AllScreens;
			for (int i = 0; i < allScreens.Length; i++)
			{
				Rectangle bounds = allScreens[i].Bounds;
				if (value.X <= (double)(bounds.X + bounds.Width) && value.X >= (double)bounds.X && value.Y <= (double)(bounds.Y + bounds.Height) && value.Y >= (double)bounds.Y)
				{
					return bounds;
				}
			}
		}
		return new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
	}

	public static double GetScalingRatio()
	{
		try
		{
			double logicalHeight = GetLogicalHeight();
			double actualHeight = GetActualHeight();
			if (logicalHeight > 0.0 && actualHeight > 0.0)
			{
				return logicalHeight / actualHeight;
			}
		}
		catch (Exception)
		{
		}
		return 1.0;
	}

	private static double GetActualHeight()
	{
		return SystemParameters.PrimaryScreenHeight;
	}

	private static double GetLogicalHeight()
	{
		double logicalHeight = 0.0;
		WindowsMonitorAPI.EnumDisplayMonitors(lpfnEnum: delegate(IntPtr m, IntPtr h, IntPtr lm, IntPtr lp)
		{
			WindowsMonitorAPI.MONITORINFOEX mONITORINFOEX = new WindowsMonitorAPI.MONITORINFOEX();
			WindowsMonitorAPI.GetMonitorInfo(new HandleRef(null, m), mONITORINFOEX);
			if ((mONITORINFOEX.dwFlags & 1) != 0)
			{
				logicalHeight = mONITORINFOEX.rcMonitor.bottom - mONITORINFOEX.rcMonitor.top;
			}
			return true;
		}, hdc: WindowsMonitorAPI.NullHandleRef, rcClip: null, dwData: IntPtr.Zero);
		return logicalHeight;
	}

	public static Rectangle GetMousePositionRect()
	{
		System.Drawing.Point mousePosition = Control.MousePosition;
		Rectangle pointLocationScreen = GetPointLocationScreen(new System.Windows.Point(mousePosition.X, mousePosition.Y), null);
		double scalingRatio = GetScalingRatio();
		return new Rectangle((int)((double)pointLocationScreen.Left / scalingRatio), (int)((double)pointLocationScreen.Top / scalingRatio), (int)((double)pointLocationScreen.Width / scalingRatio), (int)((double)pointLocationScreen.Height / scalingRatio));
	}

	public static Rectangle GetWindowPositionRect(Window window)
	{
		double num = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice.M22 ?? GetScalingRatio();
		System.Windows.Point point = new System.Windows.Point(window.Left * num, window.Top * num);
		Rectangle pointLocationScreen = GetPointLocationScreen(new System.Windows.Point(point.X, point.Y), null);
		return new Rectangle((int)((double)pointLocationScreen.Left / num), (int)((double)pointLocationScreen.Top / num), (int)((double)pointLocationScreen.Width / num), (int)((double)pointLocationScreen.Height / num));
	}

	public static Screen GetPointScreen(System.Windows.Point point)
	{
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen in allScreens)
		{
			if (screen.WorkingArea.Contains(new System.Drawing.Point((int)point.X, (int)point.Y)))
			{
				return screen;
			}
		}
		return null;
	}

	public static double GetPointDpi(System.Windows.Point point)
	{
		if (Utils.IsWindows7())
		{
			Screen[] allScreens = Screen.AllScreens;
			foreach (Screen screen in allScreens)
			{
				if (screen.Bounds.Left == 0 && screen.Bounds.Top == 0)
				{
					return (double)screen.Bounds.Width / SystemParameters.PrimaryScreenWidth;
				}
			}
			return 1.0;
		}
		Screen pointScreen = GetPointScreen(point);
		if (pointScreen != null)
		{
			pointScreen.GetDpi(DpiType.Effective, out var dpiX, out var _);
			return (double)dpiX / 96.0;
		}
		return 1.0;
	}
}
