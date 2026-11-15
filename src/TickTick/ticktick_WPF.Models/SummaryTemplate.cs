using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class SummaryTemplate
{
	public string id { get; set; }

	public string name { get; set; }

	public long sortOrder { get; set; }

	public string sortType { get; set; }

	public string rule { get; set; }

	public List<SummaryDisplayItem> displayItems { get; set; }
}
