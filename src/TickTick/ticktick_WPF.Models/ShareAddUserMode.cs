namespace ticktick_WPF.Models;

public class ShareAddUserMode
{
	public string recordId { get; set; }

	public int? toUserId { get; set; }

	public string toUserCode { get; set; }

	public string toUsername { get; set; }

	public int fromUserId { get; set; }

	public string fromUsername { get; set; }

	public bool accepted { get; set; }

	public long createdTime { get; set; }

	public long? acceptedTime { get; set; }

	public string permission { get; set; }
}
