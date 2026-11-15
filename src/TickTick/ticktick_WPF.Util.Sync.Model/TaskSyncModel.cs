using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class TaskSyncModel
{
	private AttachmentSyncBean attachmentSyncBean = new AttachmentSyncBean();

	private TaskSyncBean taskSyncBean = new TaskSyncBean();

	private TaskSyncedJsonBean taskSyncedJsonBean = new TaskSyncedJsonBean();

	public TaskSyncBean TaskSyncBean
	{
		get
		{
			return taskSyncBean;
		}
		set
		{
			taskSyncBean = value;
		}
	}

	public TaskSyncedJsonBean TaskSyncedJsonBean
	{
		get
		{
			return taskSyncedJsonBean;
		}
		set
		{
			taskSyncedJsonBean = value;
		}
	}

	public AttachmentSyncBean AttachmentSyncBean
	{
		get
		{
			return attachmentSyncBean;
		}
		set
		{
			attachmentSyncBean = value;
		}
	}
}
