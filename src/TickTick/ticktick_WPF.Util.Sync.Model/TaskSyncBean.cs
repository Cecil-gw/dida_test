using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class TaskSyncBean
{
	[JsonProperty(PropertyName = "add")]
	public List<TaskModel> Added { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "update")]
	public List<TaskModel> Updated { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "updating")]
	public List<TaskModel> Updating { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "deletedInTrash")]
	public List<TaskModel> DeletedInTrash { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "deletedForever")]
	public List<TaskModel> DeletedForever { get; set; } = new List<TaskModel>();

	public bool Empty
	{
		get
		{
			if (Added.Count == 0 && Updated.Count == 0 && Updating.Count == 0 && DeletedInTrash.Count == 0)
			{
				return DeletedForever.Count == 0;
			}
			return false;
		}
	}

	public List<string> GetIds()
	{
		List<string> list = new List<string>();
		if (Added.Any())
		{
			list.AddRange(Added.Select((TaskModel t) => t.id));
		}
		if (Updated.Any())
		{
			list.AddRange(Updated.Select((TaskModel t) => t.id));
		}
		if (Updating.Any())
		{
			list.AddRange(Updating.Select((TaskModel t) => t.id));
		}
		if (DeletedInTrash.Any())
		{
			list.AddRange(DeletedInTrash.Select((TaskModel t) => t.id));
		}
		if (DeletedForever.Any())
		{
			list.AddRange(DeletedForever.Select((TaskModel t) => t.id));
		}
		return list;
	}
}
