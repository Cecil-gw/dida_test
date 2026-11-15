using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using TickTickUtils.Lang;
using WindowChrome;

namespace TickTickUtils;

public static class WindowUtils
{
	public static void SetWindowChrome(Window window, Thickness resizeBorderThickness)
	{
		SetWindowChrome(window, new Thickness(0.01), 0.0, new CornerRadius(0.0), useAeroCaptionButtons: false, resizeBorderThickness);
	}

	public static void SetCustomWindowChrome(Window window, Thickness resizeBorderThickness)
	{
		SetCustomWindowChrome(window, new Thickness(0.01), 0.0, new CornerRadius(0.0), useAeroCaptionButtons: false, resizeBorderThickness);
	}

	private static void SetCustomWindowChrome(Window window, Thickness glassFrameThickness, double captionHeight, CornerRadius cornerRadius, bool useAeroCaptionButtons, Thickness resizeBorderThickness)
	{
		UtilLog.Info("TickTick.WindowChrome");
		global::WindowChrome.WindowChrome chrome = new global::WindowChrome.WindowChrome
		{
			GlassFrameThickness = glassFrameThickness,
			CaptionHeight = captionHeight,
			CornerRadius = cornerRadius,
			UseAeroCaptionButtons = useAeroCaptionButtons,
			ResizeBorderThickness = resizeBorderThickness
		};
		global::WindowChrome.WindowChrome.SetWindowChrome(window, chrome);
	}

	private static void SetWindowChrome(Window window, Thickness glassFrameThickness, double captionHeight, CornerRadius cornerRadius, bool useAeroCaptionButtons, Thickness resizeBorderThickness)
	{
		if (GlobalSettings.UseCustomWindowChrome)
		{
			SetCustomWindowChrome(window, glassFrameThickness, captionHeight, cornerRadius, useAeroCaptionButtons, resizeBorderThickness);
			return;
		}
		UtilLog.Info("System.Windows.Shell.WindowChrome");
		System.Windows.Shell.WindowChrome chrome = new System.Windows.Shell.WindowChrome
		{
			GlassFrameThickness = glassFrameThickness,
			CaptionHeight = captionHeight,
			CornerRadius = cornerRadius,
			UseAeroCaptionButtons = useAeroCaptionButtons,
			ResizeBorderThickness = resizeBorderThickness
		};
		System.Windows.Shell.WindowChrome.SetWindowChrome(window, chrome);
		window.Loaded += OnWindowLoaded;
	}

	private static void OnWindowLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is Window window)
		{
			window.Loaded -= OnWindowLoaded;
			((HwndSource)PresentationSource.FromVisual(window))?.AddHook(HookProc);
		}
	}

	private static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 132 || msg == 165)
		{
			try
			{
				lParam.ToInt32();
			}
			catch (OverflowException)
			{
				handled = true;
			}
		}
		return IntPtr.Zero;
	}

	public static void EnableBlur(Window window, bool enable)
	{
		try
		{
			if (!EnvironmentUtils.IsWindows7() && !EnvironmentUtils.IsWindows11())
			{
				WindowInteropHelper windowInteropHelper = new WindowInteropHelper(window);
				CommonConstants.AccentPolicy structure = new CommonConstants.AccentPolicy
				{
					AccentState = (enable ? CommonConstants.AccentState.ACCENT_ENABLE_BLURBEHIND : CommonConstants.AccentState.ACCENT_DISABLED)
				};
				int num = Marshal.SizeOf(structure);
				IntPtr intPtr = Marshal.AllocHGlobal(num);
				Marshal.StructureToPtr(structure, intPtr, fDeleteOld: false);
				CommonConstants.WindowCompositionAttributeData data = new CommonConstants.WindowCompositionAttributeData
				{
					Attribute = CommonConstants.WindowCompositionAttribute.WCA_ACCENT_POLICY,
					SizeOfData = num,
					Data = intPtr
				};
				SetWindowCompositionAttribute(windowInteropHelper.Handle, ref data);
				Marshal.FreeHGlobal(intPtr);
			}
		}
		catch (Exception)
		{
		}
	}

	[DllImport("user32.dll")]
	private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref CommonConstants.WindowCompositionAttributeData data);

	public static void SetWindowChromeBorderThickness(Window window, Thickness thickness)
	{
		global::WindowChrome.WindowChrome windowChrome = global::WindowChrome.WindowChrome.GetWindowChrome(window);
		if (windowChrome != null)
		{
			windowChrome.ResizeBorderThickness = thickness;
			return;
		}
		System.Windows.Shell.WindowChrome windowChrome2 = System.Windows.Shell.WindowChrome.GetWindowChrome(window);
		if (windowChrome2 != null)
		{
			windowChrome2.ResizeBorderThickness = thickness;
		}
	}
}
