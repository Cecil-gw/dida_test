using System.IO;
using System.Text;

namespace ticktick_WPF.Util;

internal class DebugWriter : TextWriter
{
	public override Encoding Encoding => Encoding.UTF8;

	public override void WriteLine()
	{
	}

	public override void WriteLine(string s)
	{
	}
}
