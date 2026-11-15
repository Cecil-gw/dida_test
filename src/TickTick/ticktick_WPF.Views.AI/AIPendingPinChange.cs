using System;

namespace ticktick_WPF.Views.AI;

internal sealed class AIPendingPinChange
{
	public AIPinChangeKind Kind { get; set; }

	public long SortOrder { get; set; }

	public string AnchorId { get; set; }

	public string Revision { get; set; } = Guid.NewGuid().ToString();
}
