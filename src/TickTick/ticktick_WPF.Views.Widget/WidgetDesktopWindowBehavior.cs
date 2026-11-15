using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using TickTickUtils.Lang;
using ticktick_WPF.Resource;
using ticktick_WPF.Util;

namespace ticktick_WPF.Views.Widget;

internal sealed class WidgetDesktopWindowBehavior : IDisposable
{
	private static readonly DependencyProperty BehaviorProperty = DependencyProperty.RegisterAttached("DesktopWindowBehavior", typeof(WidgetDesktopWindowBehavior), typeof(WidgetDesktopWindowBehavior));

	private readonly Window _window;

	private readonly Func<string> _widgetType;

	private readonly Func<string> _displayOption;

	private HwndSource _source;

	private IntPtr _originalOwner;

	private DateTime _lastOwnerDiagnostic = DateTime.MinValue;

	private bool _ownerDiagnosticPending;

	private bool _disposed;

	private WidgetDesktopWindowBehavior(Window window, Func<string> widgetType, Func<string> displayOption)
	{
		_window = window;
		_widgetType = widgetType;
		_displayOption = displayOption;
		window.SourceInitialized += OnSourceInitialized;
		window.Closed += OnClosed;
	}

	internal static WidgetDesktopWindowBehavior Attach(Window window, Func<string> widgetType, Func<string> displayOption)
	{
		WidgetDesktopWindowBehavior widgetDesktopWindowBehavior = (WidgetDesktopWindowBehavior)window.GetValue(BehaviorProperty);
		if (widgetDesktopWindowBehavior != null)
		{
			return widgetDesktopWindowBehavior;
		}
		widgetDesktopWindowBehavior = new WidgetDesktopWindowBehavior(window, widgetType, displayOption);
		window.SetValue(BehaviorProperty, widgetDesktopWindowBehavior);
		if (new WindowInteropHelper(window).Handle != IntPtr.Zero)
		{
			widgetDesktopWindowBehavior.Initialize();
		}
		return widgetDesktopWindowBehavior;
	}

	private void OnSourceInitialized(object sender, EventArgs args)
	{
		Initialize();
	}

	private void Initialize()
	{
		if (!_disposed && _source == null)
		{
			IntPtr handle = new WindowInteropHelper(_window).Handle;
			try
			{
				_originalOwner = NativeUtils.GetWindowOwner(handle);
				int windowLong = NativeUtils.GetWindowLong(handle, -20);
				NativeUtils.SetWindowLong(handle, WinParameter.GWL_EXSTYLE, windowLong | 0x80);
				_source = HwndSource.FromHwnd(handle);
				_source?.AddHook(WndProc);
			}
			catch (Exception ex)
			{
				UtilLog.Error("Widget GetWindowLong Error:" + ex);
			}
			UpdateDisplayOption();
		}
	}

	internal void UpdateDisplayOption()
	{
		if (_disposed)
		{
			return;
		}
		_window.Topmost = _displayOption() == "top";
		if (_source == null)
		{
			return;
		}
		if (_window.IsVisible)
		{
			_window.Activate();
		}
		try
		{
			ApplyDesktopOwner();
			if (_window.Topmost)
			{
				NativeUtils.RestoreTopmost(_window);
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("Widget SetWindowLong Error:" + ex);
		}
	}

	private bool ApplyDesktopOwner()
	{
		if (_disposed || _source == null)
		{
			return false;
		}
		IntPtr handle = _source.Handle;
		LogDesktopOwner("before-apply");
		int num;
		IntPtr intPtr;
		if (!_window.Topmost)
		{
			num = (LocalSettings.Settings.ExtraSettings.DisableWidgetDesktopOwner ? 1 : 0);
			if (num == 0)
			{
				intPtr = NativeUtils.GetDesktopPtr();
				goto IL_0058;
			}
		}
		else
		{
			num = 1;
		}
		intPtr = _originalOwner;
		goto IL_0058;
		IL_0058:
		IntPtr intPtr2 = intPtr;
		if (num == 0 && intPtr2 == IntPtr.Zero)
		{
			UtilLog.Error("WidgetDesktopOwner apply failed: desktop window not found");
			return false;
		}
		int num2 = NativeUtils.SetWindowOwner(handle, intPtr2);
		bool flag = NativeUtils.GetWindowOwner(handle) == intPtr2;
		UtilLog.Info($"WidgetDesktopOwner apply hwnd=0x{handle.ToInt64():X}, requested=0x{intPtr2.ToInt64():X}, win32Error={num2}, matched={flag}");
		LogDesktopOwner("after-apply");
		return num2 == 0 && flag;
	}

	internal static int SetDesktopOwnerCompatibility(bool disabled)
	{
		WidgetDesktopWindowBehavior[] array = (from Window window in System.Windows.Application.Current.Windows
			select (WidgetDesktopWindowBehavior)window.GetValue(BehaviorProperty) into behavior
			where behavior != null
			select behavior).ToArray();
		WidgetDesktopWindowBehavior[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			array2[num].LogDesktopOwner("compatibility-before-switch");
		}
		LocalSettings.Settings.ExtraSettings.DisableWidgetDesktopOwner = disabled;
		UtilLog.Info($"WidgetDesktopOwner compatibility-switch disabled={disabled}, widgets={array.Length}");
		int num2 = 0;
		array2 = array;
		foreach (WidgetDesktopWindowBehavior widgetDesktopWindowBehavior in array2)
		{
			try
			{
				if (!widgetDesktopWindowBehavior.ApplyDesktopOwner())
				{
					num2++;
				}
			}
			catch (Exception ex)
			{
				num2++;
				UtilLog.Error("WidgetDesktopOwner apply failed: " + ex);
			}
		}
		UtilLog.Info($"WidgetDesktopOwner compatibility-result disabled={disabled}, widgets={array.Length}, failed={num2}");
		return num2;
	}

	private void LogDesktopOwner(string reason)
	{
		try
		{
			IntPtr handle = new WindowInteropHelper(_window).Handle;
			if (!(handle == IntPtr.Zero))
			{
				string text = string.Join(";", Screen.AllScreens.Select((Screen screen) => $"{screen.Bounds}/primary={screen.Primary}"));
				UtilLog.Info($"WidgetDesktopOwner {reason}, disabled={LocalSettings.Settings.ExtraSettings.DisableWidgetDesktopOwner}, " + $"type={_widgetType()}, display={_displayOption()}, topmost={_window.Topmost}, os={Environment.OSVersion.Version}, screens={text}, " + NativeUtils.DescribeWidgetDesktop(handle));
			}
		}
		catch (Exception ex)
		{
			UtilLog.Error("WidgetDesktopOwner diagnostic failed: " + ex);
		}
	}

	private async void CaptureDesktopOwnerInteraction(string reason)
	{
		if (_disposed || _ownerDiagnosticPending || DateTime.UtcNow - _lastOwnerDiagnostic < TimeSpan.FromSeconds(5.0))
		{
			return;
		}
		_lastOwnerDiagnostic = DateTime.UtcNow;
		_ownerDiagnosticPending = true;
		try
		{
			LogDesktopOwner(reason + "-before");
			await Task.Delay(250);
			if (!_disposed && _window.IsLoaded)
			{
				LogDesktopOwner(reason + "-after");
			}
		}
		finally
		{
			_ownerDiagnosticPending = false;
		}
	}

	private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 33)
		{
			CaptureDesktopOwnerInteraction("mouse-activate");
		}
		else if (msg == 513)
		{
			CaptureDesktopOwnerInteraction("mouse-down");
		}
		else if (msg == 6 && (wParam.ToInt64() & 0xFFFF) != 0L)
		{
			CaptureDesktopOwnerInteraction("activate");
		}
		else if (msg == 126)
		{
			CaptureDesktopOwnerInteraction("display-change");
		}
		if (msg == 28 && wParam != IntPtr.Zero && _displayOption() == "top")
		{
			NativeUtils.RestoreTopmost(_window);
		}
		return IntPtr.Zero;
	}

	private void OnClosed(object sender, EventArgs args)
	{
		Dispose();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_window.SourceInitialized -= OnSourceInitialized;
			_window.Closed -= OnClosed;
			_source?.RemoveHook(WndProc);
			_source = null;
			_window.ClearValue(BehaviorProperty);
		}
	}
}
