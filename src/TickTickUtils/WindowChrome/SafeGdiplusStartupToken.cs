using System;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;

namespace WindowChrome;

internal sealed class SafeGdiplusStartupToken : SafeHandleZeroOrMinusOneIsInvalid
{
	private SafeGdiplusStartupToken()
		: base(ownsHandle: true)
	{
	}

	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	protected override bool ReleaseHandle()
	{
		return NativeMethods.GdiplusShutdown(handle) == Status.Ok;
	}

	public static SafeGdiplusStartupToken Startup()
	{
		SafeGdiplusStartupToken safeGdiplusStartupToken = new SafeGdiplusStartupToken();
		if (NativeMethods.GdiplusStartup(out var token, new StartupInput(), out var _) == Status.Ok)
		{
			safeGdiplusStartupToken.handle = token;
			return safeGdiplusStartupToken;
		}
		safeGdiplusStartupToken.Dispose();
		throw new Exception("Unable to initialize GDI+");
	}
}
