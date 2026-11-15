using System;

namespace WindowChrome;

[Flags]
public enum NonClientFrameEdges
{
	None = 0,
	Left = 1,
	Top = 2,
	Right = 4,
	Bottom = 8
}
