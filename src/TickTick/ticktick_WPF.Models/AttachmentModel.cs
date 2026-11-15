using System;
using TickTickModels;

namespace ticktick_WPF.Models;

public class AttachmentModel : BaseModel
{
	public string id { get; set; }

	public string refId { get; set; }

	public string path { get; set; }

	public string localPath { get; set; }

	public int size { get; set; }

	public string fileName { get; set; }

	public string fileType { get; set; }

	public DateTime? createdTime { get; set; }

	public string taskId { get; set; }

	public bool deleted { get; set; }

	public bool canDelete { get; set; }

	public string sync_status { get; set; }

	public int status { get; set; }

	public bool IsActive()
	{
		if (!deleted)
		{
			return status == 0;
		}
		return false;
	}

	public static AttachmentModel Copy(AttachmentModel other)
	{
		return other.MemberwiseClone() as AttachmentModel;
	}
}
