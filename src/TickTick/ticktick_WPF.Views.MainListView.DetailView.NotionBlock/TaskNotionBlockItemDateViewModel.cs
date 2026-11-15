using System;

namespace ticktick_WPF.Views.MainListView.DetailView.NotionBlock;

public class TaskNotionBlockItemDateViewModel
{
	public DateTime? start { get; set; }

	public DateTime? end { get; set; }

	public bool isAllDay { get; set; }

	public string timeZone { get; set; }
}
