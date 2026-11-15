using System.ComponentModel;

namespace ticktick_WPF.ViewModels;

internal enum DateFilterType
{
	[Description("All")]
	All,
	[Description("Today")]
	Today,
	[Description("PublicYesterday")]
	Yesterday,
	[Description("ThisWeek")]
	ThisWeek,
	[Description("ThisMonth")]
	ThisMonth,
	[Description("LastMonth")]
	LastMonth,
	[Description("Custom")]
	Custom
}
