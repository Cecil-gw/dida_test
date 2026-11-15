using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class GuideProjectData
{
	public string sortType { get; set; }

	public string viewMode { get; set; }

	public List<GuideProjectColumn> columns { get; set; }

	public List<GuideProjectTask> tasks { get; set; }
}
