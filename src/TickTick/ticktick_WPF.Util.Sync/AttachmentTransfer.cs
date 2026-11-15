using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class AttachmentTransfer
{
	public static AttachmentModel ConvertServerToLocal(AttachmentModel remote, string taskId)
	{
		return new AttachmentModel
		{
			id = remote.id,
			taskId = taskId,
			fileType = remote.fileType,
			fileName = remote.fileName,
			status = remote.status,
			path = remote.path,
			createdTime = remote.createdTime,
			size = remote.size,
			refId = remote.refId,
			sync_status = 2.ToString()
		};
	}
}
