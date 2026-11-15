using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Remind;

internal static class ReminderWindowPositionHelper
{
	private const double DefaultWidth = 324.0;

	private const double DefaultHeight = 172.0;

	private const double BottomMargin = 8.0;

	private const double HiddenBottomMargin = 24.0;

	public static void ResetToBottomRight(Window window)
	{
		if (window != null)
		{
			Rect primaryWorkAreaDip = GetPrimaryWorkAreaDip();
			window.Left = primaryWorkAreaDip.Left + primaryWorkAreaDip.Width - GetWindowWidth(window);
			window.Top = GetStackTop(primaryWorkAreaDip, window, 0);
		}
	}

	public static void RefreshHorizontalPosition(Window window)
	{
		if (window != null)
		{
			Rect primaryWorkAreaDip = GetPrimaryWorkAreaDip();
			window.Left = primaryWorkAreaDip.Left + primaryWorkAreaDip.Width - GetWindowWidth(window);
		}
	}

	public static double GetStackTop(Window window, int index)
	{
		return GetStackTop(GetPrimaryWorkAreaDip(), window, index);
	}

	public static double GetBaseTop(Window window)
	{
		Rect primaryWorkAreaDip = GetPrimaryWorkAreaDip();
		return primaryWorkAreaDip.Top + primaryWorkAreaDip.Height - GetWindowHeight(window) - 8.0;
	}

	public static double GetHiddenTop(Window window)
	{
		Rect primaryWorkAreaDip = GetPrimaryWorkAreaDip();
		return primaryWorkAreaDip.Top + primaryWorkAreaDip.Height - GetWindowHeight(window) - 24.0;
	}

	private static double GetStackTop(Rect area, Window window, int index)
	{
		return area.Top + area.Height - GetWindowHeight(window) - 8.0 - (double)(8 * index);
	}

	private static Rect GetPrimaryWorkAreaDip()
	{
		Screen primaryScreen = Screen.PrimaryScreen;
		if (primaryScreen == null)
		{
			return new Rect(0.0, 0.0, SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
		}
		Rectangle workingArea = primaryScreen.WorkingArea;
		double num = 1.0;
		double num2 = 1.0;
		if (!Utils.IsWindows7())
		{
			primaryScreen.GetDpi(DpiType.Effective, out var dpiX, out var dpiY);
			num = (double)dpiX / 96.0;
			num2 = (double)dpiY / 96.0;
		}
		return new Rect((double)workingArea.Left / num, (double)workingArea.Top / num2, (double)workingArea.Width / num, (double)workingArea.Height / num2);
	}

	private static double GetWindowWidth(Window window)
	{
		if (!double.IsNaN(window.Width) && window.Width > 0.0)
		{
			return window.Width;
		}
		if (!(window.ActualWidth > 0.0))
		{
			return 324.0;
		}
		return window.ActualWidth;
	}

	private static double GetWindowHeight(Window window)
	{
		if (!double.IsNaN(window.Height) && window.Height > 0.0)
		{
			return window.Height;
		}
		if (!(window.ActualHeight > 0.0))
		{
			return 172.0;
		}
		return window.ActualHeight;
	}
}
