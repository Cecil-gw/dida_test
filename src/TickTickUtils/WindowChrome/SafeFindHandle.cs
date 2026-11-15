using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace WindowChrome;

internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
	private SafeFindHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return NativeMethods.FindClose(handle);
	}
}
