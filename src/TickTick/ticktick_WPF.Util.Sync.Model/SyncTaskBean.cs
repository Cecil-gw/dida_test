using System.Collections.Generic;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync.Model;

public class SyncTaskBean
{
	[JsonProperty(PropertyName = "update")]
	public List<TaskModel> Update { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "delete")]
	public List<TaskProject> Delete { get; set; } = new List<TaskProject>();

	[JsonProperty(PropertyName = "add")]
	public List<TaskModel> Add { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "deletedInTrash")]
	public List<TaskProject> DeletedInTrash { get; set; } = new List<TaskProject>();

	[JsonProperty(PropertyName = "deletedForever")]
	public List<TaskProject> DeletedForever { get; set; } = new List<TaskProject>();

	[JsonIgnore]
	public List<TaskProject> DeletedFromTrash { get; set; } = new List<TaskProject>();

	[JsonProperty(PropertyName = "addAttachments")]
	public List<TaskModel> AddAttachments { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "updateAttachments")]
	public List<TaskModel> UpdateAttachments { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "tagUpdate")]
	public List<TaskModel> TagUpdate { get; set; } = new List<TaskModel>();

	[JsonProperty(PropertyName = "deleteAttachments")]
	public List<TaskModel> DeleteAttachments { get; set; } = new List<TaskModel>();

	public bool Empty
	{
		get
		{
			if (Add.Count == 0 && TagUpdate.Count == 0 && Update.Count == 0 && Delete.Count == 0 && DeletedInTrash.Count == 0)
			{
				return DeletedForever.Count == 0;
			}
			return false;
		}
	}

	public string GetCountMessage()
	{
		return Update?.Count + " " + Delete?.Count + " " + Add?.Count + " " + DeletedInTrash?.Count + " " + DeletedForever?.Count + " " + DeletedFromTrash?.Count + "  " + AddAttachments?.Count + "  " + UpdateAttachments?.Count + "  " + DeleteAttachments?.Count;
	}
}
