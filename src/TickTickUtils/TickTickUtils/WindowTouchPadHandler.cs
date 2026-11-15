using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;

namespace TickTickUtils;

public class WindowTouchPadHandler
{
	private static readonly Dictionary<Window, WindowTouchPadHandler> Handlers = new Dictionary<Window, WindowTouchPadHandler>();

	private Action<bool> _action;

	private Window _window;

	private WindowTouchPadHandler(Window window, Action<bool> action)
	{
		_action = action;
		_window = window;
	}

	public static void AddHook(Window window, Action<bool> action)
	{
		if (window != null)
		{
			WindowTouchPadHandler windowTouchPadHandler = null;
			if (Handlers.ContainsKey(window))
			{
				windowTouchPadHandler = Handlers[window];
			}
			else
			{
				windowTouchPadHandler = new WindowTouchPadHandler(window, action);
				Handlers[window] = windowTouchPadHandler;
			}
			windowTouchPadHandler.AddHook();
		}
	}

	public static void RemoveHook(Window window)
	{
		if (window != null && Handlers.ContainsKey(window))
		{
			WindowTouchPadHandler windowTouchPadHandler = Handlers[window];
			Handlers.Remove(window);
			windowTouchPadHandler.RemoveHook();
		}
	}

	private void RemoveHook()
	{
		((HwndSource)PresentationSource.FromVisual(_window))?.RemoveHook(Hook);
		_window = null;
		_action = null;
	}

	private void AddHook()
	{
		((HwndSource)PresentationSource.FromVisual(_window))?.AddHook(Hook);
	}

	private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		if (msg == 526)
		{
			int num = 0;
			num = ((!CommonConstants.IsX64) ? ((short)HIWORD(wParam)) : LOWORD(wParam));
			_action(num > 0);
			return (IntPtr)1;
		}
		return IntPtr.Zero;
	}

	private static int HIWORD(IntPtr ptr)
	{
		return (ptr.ToInt32() >> 16) & 0xFFFF;
	}

	private static int LOWORD(IntPtr ptr)
	{
		return ((int)ptr.ToInt64() >> 16) % 256;
	}
}
