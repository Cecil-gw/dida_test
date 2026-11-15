using System;
using System.Runtime.InteropServices;

namespace WindowChrome;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal class SHARDAPPIDINFOIDLIST
{
	private IntPtr pidl;

	[MarshalAs(UnmanagedType.LPWStr)]
	private string pszAppID;
}
