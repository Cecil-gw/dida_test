using System;
using System.Runtime.InteropServices;
using System.Security;

namespace ticktick_WPF.Util;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
[SuppressUnmanagedCodeSecurity]
public interface IVirtualDesktopManager
{
	[PreserveSig]
	int IsWindowOnCurrentVirtualDesktop([In] IntPtr TopLevelWindow, out int OnCurrentDesktop);

	[PreserveSig]
	int GetWindowDesktopId([In] IntPtr TopLevelWindow, out Guid CurrentDesktop);

	[PreserveSig]
	int MoveWindowToDesktop([In] IntPtr TopLevelWindow, [In][MarshalAs(UnmanagedType.LPStruct)] Guid CurrentDesktop);
}
