using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ticktick_WPF.Views.Config;

namespace ticktick_WPF.Util;

public class GlobalHotkey : IDisposable
{
	public delegate bool KeyboardCallback(KeyEvent keyEvent, int vkCode, SpecialKeyState state);

	private static GlobalHotkey instance;

	private InterceptKeys.LowLevelKeyboardProc hookedLowLevelKeyboardProc;

	private IntPtr hookId = IntPtr.Zero;

	private const int VK_SHIFT = 16;

	private const int VK_CONTROL = 17;

	private const int VK_ALT = 18;

	private const int VK_WIN = 91;

	public static GlobalHotkey Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new GlobalHotkey();
			}
			return instance;
		}
	}

	public event KeyboardCallback hookedKeyboardCallback;

	private GlobalHotkey()
	{
		hookedLowLevelKeyboardProc = LowLevelKeyboardProc;
		hookId = InterceptKeys.SetHook(hookedLowLevelKeyboardProc);
	}

	public SpecialKeyState CheckModifiers()
	{
		SpecialKeyState specialKeyState = new SpecialKeyState();
		if ((InterceptKeys.GetKeyState(16) & 0x8000) != 0)
		{
			specialKeyState.ShiftPressed = true;
		}
		if ((InterceptKeys.GetKeyState(17) & 0x8000) != 0)
		{
			specialKeyState.CtrlPressed = true;
		}
		if ((InterceptKeys.GetKeyState(18) & 0x8000) != 0)
		{
			specialKeyState.AltPressed = true;
		}
		if ((InterceptKeys.GetKeyState(91) & 0x8000) != 0)
		{
			specialKeyState.WinPressed = true;
		}
		return specialKeyState;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
	{
		bool flag = true;
		if (nCode >= 0 && (wParam.ToUInt32() == 256 || wParam.ToUInt32() == 257 || wParam.ToUInt32() == 260 || wParam.ToUInt32() == 261) && this.hookedKeyboardCallback != null)
		{
			flag = this.hookedKeyboardCallback((KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam), CheckModifiers());
		}
		if (flag)
		{
			return InterceptKeys.CallNextHookEx(hookId, nCode, wParam, lParam);
		}
		return (IntPtr)1;
	}

	~GlobalHotkey()
	{
		Dispose();
	}

	public void Dispose()
	{
		InterceptKeys.UnhookWindowsHookEx(hookId);
	}
}
