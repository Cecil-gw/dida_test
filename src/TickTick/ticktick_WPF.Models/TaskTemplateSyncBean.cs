using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class TaskTemplateSyncBean
{
	[JsonProperty(PropertyName = "add")]
	public List<TaskTemplateModel> Add { get; set; } = new List<TaskTemplateModel>();

	[JsonProperty(PropertyName = "delete")]
	public List<string> Delete { get; set; } = new List<string>();

	[JsonProperty(PropertyName = "update")]
	public List<TaskTemplateModel> Update { get; set; } = new List<TaskTemplateModel>();

	public bool IsEmpty()
	{
		List<TaskTemplateModel> add = Add;
		if (add != null && add.Count == 0)
		{
			List<string> delete = Delete;
			if (delete != null && delete.Count == 0)
			{
				List<TaskTemplateModel> update = Update;
				if (update == null)
				{
					return false;
				}
				return update.Count == 0;
			}
		}
		return false;
	}
}
