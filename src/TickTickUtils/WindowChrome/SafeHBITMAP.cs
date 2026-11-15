using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;

namespace WindowChrome;

internal sealed class SafeHBITMAP : SafeHandleZeroOrMinusOneIsInvalid
{
	private SafeHBITMAP()
		: base(ownsHandle: true)
	{
	}

	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	protected override bool ReleaseHandle()
	{
		return NativeMethods.DeleteObject(handle);
	}
}
