using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class CommonOtherSettings
{
	public Dictionary<string, string> MatrixActiveViewIds { get; set; } = new Dictionary<string, string>();

	public Dictionary<string, bool> PendingMatrixShowCompleted { get; set; } = new Dictionary<string, bool>();

	public bool? ShowParallelGuide { get; set; }

	public bool? MatrixManageViewsGuideShown { get; set; }

	public bool? MatrixSwitchViewsGuideShown { get; set; }
}
