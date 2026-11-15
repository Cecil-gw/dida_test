using Newtonsoft.Json;

namespace ticktick_WPF.Util.Sync;

public class BatchTaskOrderUpdateResult
{
	private BatchUpdateResult taskOrderByDate;

	private BatchUpdateResult projectGroup;

	[JsonProperty(PropertyName = "taskOrderByDate")]
	public BatchUpdateResult TaskOrderByDate
	{
		get
		{
			return taskOrderByDate;
		}
		set
		{
			taskOrderByDate = value;
		}
	}

	[JsonProperty(PropertyName = "taskOrderByProject")]
	public BatchUpdateResult ProjectGroup
	{
		get
		{
			return projectGroup;
		}
		set
		{
			projectGroup = value;
		}
	}
}
