using System.Runtime.InteropServices;

namespace WindowChrome;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal class SHARDAPPIDINFO
{
	[MarshalAs(UnmanagedType.Interface)]
	private object psi;

	[MarshalAs(UnmanagedType.LPWStr)]
	private string pszAppID;
}
