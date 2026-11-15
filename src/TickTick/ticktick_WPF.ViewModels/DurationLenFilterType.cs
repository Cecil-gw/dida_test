using System.ComponentModel;

namespace ticktick_WPF.ViewModels;

internal enum DurationLenFilterType
{
	[Description("All")]
	All,
	[Description("10")]
	Less10min,
	[Description("25")]
	Less25min,
	[Description("45")]
	Less45min,
	[Description("60")]
	Less60min,
	[Description("Custom")]
	Custom
}
