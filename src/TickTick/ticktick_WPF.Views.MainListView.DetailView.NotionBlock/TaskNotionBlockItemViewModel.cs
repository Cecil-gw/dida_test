using System.Collections.Generic;

namespace ticktick_WPF.Views.MainListView.DetailView.NotionBlock;

public class TaskNotionBlockItemViewModel
{
	public string key { get; set; }

	public string type { get; set; }

	public TaskNotionBlockItemSelectViewModel status { get; set; }

	public TaskNotionBlockItemSelectViewModel select { get; set; }

	public string text { get; set; }

	public double number { get; set; }

	public TaskNotionBlockItemDateViewModel date { get; set; }

	public List<TaskNotionBlockItemSelectViewModel> multi_select { get; set; }

	public List<TaskNotionBlockItemPeopleViewModel> People { get; set; }
}
