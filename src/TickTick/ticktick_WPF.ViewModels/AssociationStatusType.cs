using System.ComponentModel;

namespace ticktick_WPF.ViewModels;

internal enum AssociationStatusType
{
	[Description("All")]
	All,
	[Description("Linked")]
	Has,
	[Description("Unlinked")]
	No
}
