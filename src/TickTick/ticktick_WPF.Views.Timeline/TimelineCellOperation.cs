using System;

namespace ticktick_WPF.Views.Timeline;

[Flags]
public enum TimelineCellOperation
{
	None = 1,
	Start = 2,
	Full = 4,
	End = 8,
	Hover = 0x10,
	Hide = 0x20,
	Fold = 0x40,
	Edit = 0x80,
	BatchSelect = 0x100,
	HoverStart = 0x12,
	HoverFull = 0x14,
	HoverEnd = 0x18
}
