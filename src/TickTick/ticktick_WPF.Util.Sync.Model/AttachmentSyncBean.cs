using System.Collections.Generic;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class AttachmentSyncBean
{
	private List<AttachmentModel> added = new List<AttachmentModel>();

	private List<AttachmentModel> updated = new List<AttachmentModel>();

	private List<AttachmentModel> deleted = new List<AttachmentModel>();

	public bool Empty
	{
		get
		{
			if (added.Count == 0 && updated.Count == 0)
			{
				return deleted.Count == 0;
			}
			return false;
		}
	}

	public List<AttachmentModel> Added
	{
		get
		{
			return added;
		}
		set
		{
			added = value;
		}
	}

	public List<AttachmentModel> Updated
	{
		get
		{
			return updated;
		}
		set
		{
			updated = value;
		}
	}

	public List<AttachmentModel> Deleted
	{
		get
		{
			return deleted;
		}
		set
		{
			deleted = value;
		}
	}
}
