namespace TickTickModels;

public class TaskStickyModel : BaseModel
{
	public string TaskId { get; set; }

	public bool TopMost { get; set; }

	public bool Expand { get; set; }

	public bool Closed { get; set; }

	public double Top { get; set; } = -1.0;

	public double Left { get; set; } = -1.0;

	public double Width { get; set; }

	public double Height { get; set; }

	public string UserId { get; set; }

	public string Color { get; set; }

	public long LastCloseDate { get; set; }

	public TaskStickyModel()
	{
	}

	public TaskStickyModel(string taskId, string userId)
	{
		TaskId = taskId;
		UserId = userId;
		Width = 254.0;
		Height = 228.0;
		Expand = true;
	}

	public void ResetValue()
	{
		Left = -1.0;
		Top = -1.0;
		Width = 254.0;
		Height = 228.0;
		Expand = true;
		LastCloseDate = 0L;
	}
}
