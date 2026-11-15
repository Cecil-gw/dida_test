using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;

namespace ticktick_WPF.Util;

public class HwndHelper
{
	[DllImport("User32.dll")]
	public static extern IntPtr SetFocus(IntPtr hWnd);

	public static void SetFocus(Popup popup, bool getKeyFocus = false)
	{
		if (popup?.Child != null)
		{
			HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(popup.Child);
			if (hwndSource != null)
			{
				SetFocus(hwndSource.Handle);
			}
			if (getKeyFocus)
			{
				Keyboard.Focus(popup);
				FocusManager.SetFocusedElement(popup, popup);
			}
		}
	}

	public static void SetFocus(UIElement element)
	{
		if (element != null)
		{
			HwndSource hwndSource = (HwndSource)PresentationSource.FromVisual(element);
			if (hwndSource != null)
			{
				SetFocus(hwndSource.Handle);
			}
		}
	}
}
