using System.Collections.Generic;

namespace ticktick_WPF.Models;

public class TaskSyncedJsonBean
{
	private List<TaskModel> added = new List<TaskModel>();

	private List<TaskModel> updated = new List<TaskModel>();

	public List<TaskModel> Added
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

	public List<TaskModel> Updated
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

	public bool Empty
	{
		get
		{
			if (Added.Count == 0)
			{
				return Updated.Count == 0;
			}
			return false;
		}
	}
}
