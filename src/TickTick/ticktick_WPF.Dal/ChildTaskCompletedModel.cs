namespace ticktick_WPF.Dal;

internal class ChildTaskCompletedModel
{
	public string parentId { get; set; }

	public int status { get; set; }

	public string projectId { get; set; }
}
