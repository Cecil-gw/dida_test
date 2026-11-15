using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ticktick_WPF.Util;

public static class KeyboardHelper
{
	private const uint MAPVK_VK_TO_VSC = 0u;

	private const int VK_CONTROL = 17;

	private const int VK_MENU = 18;

	private const int VK_SHIFT = 16;

	private const int VK_CAPITAL = 20;

	[DllImport("user32.dll")]
	private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

	[DllImport("user32.dll")]
	private static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

	[DllImport("user32.dll")]
	private static extern IntPtr GetKeyboardLayout(uint idThread);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	public static char? GetCharFromWpfKey(KeyEventArgs e, Window window)
	{
		IntPtr handle = new WindowInteropHelper(window).Handle;
		if (handle == IntPtr.Zero)
		{
			return null;
		}
		uint lpdwProcessId;
		IntPtr keyboardLayout = GetKeyboardLayout(GetWindowThreadProcessId(handle, out lpdwProcessId));
		int num = KeyInterop.VirtualKeyFromKey(e.Key);
		if (num <= 0)
		{
			return null;
		}
		byte[] array = new byte[256];
		ModifierKeys modifiers = Keyboard.Modifiers;
		if (modifiers.HasFlag(ModifierKeys.Control))
		{
			array[17] = 128;
		}
		if (modifiers.HasFlag(ModifierKeys.Alt))
		{
			array[18] = 128;
		}
		if (modifiers.HasFlag(ModifierKeys.Shift))
		{
			array[16] = 128;
		}
		if ((GetKeyState(20) & 1) != 0)
		{
			array[20] = 1;
		}
		array[num] = 128;
		uint wScanCode = MapVirtualKeyEx((uint)num, 0u, keyboardLayout);
		StringBuilder stringBuilder = new StringBuilder(5);
		if (ToUnicodeEx((uint)num, wScanCode, array, stringBuilder, stringBuilder.Capacity, 0u, keyboardLayout) > 0 && stringBuilder.Length > 0)
		{
			return stringBuilder[0];
		}
		return null;
	}

	[DllImport("user32.dll")]
	private static extern short GetKeyState(int nVirtKey);

	public static bool IsLikelyAltGrInput(char c)
	{
		if (char.IsLetterOrDigit(c))
		{
			return false;
		}
		if (char.IsWhiteSpace(c))
		{
			return false;
		}
		if (char.IsControl(c))
		{
			return false;
		}
		return true;
	}
}
