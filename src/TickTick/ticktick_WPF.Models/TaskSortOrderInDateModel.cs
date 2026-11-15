using TickTickModels;

namespace ticktick_WPF.Models;

public class TaskSortOrderInDateModel : BaseModel
{
	public string userid { get; set; }

	public string date { get; set; }

	public string projectid { get; set; }

	public string taskid { get; set; }

	public long sortOrder { get; set; }

	public long modifiedTime { get; set; }

	public int status { get; set; }

	public int type { get; set; }

	public int deleted { get; set; }
}
