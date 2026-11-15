using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SQLite;
using TickTickModels;

namespace ticktick_WPF.Models;

internal class ProjectTemplatesModel : BaseModel
{
	public long Timestamp { get; set; }

	public string ProjectTemplatesJson
	{
		get
		{
			if (ProjectTemplates == null)
			{
				return null;
			}
			return JsonConvert.SerializeObject(ProjectTemplates);
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				ProjectTemplates = null;
			}
			else
			{
				ProjectTemplates = JsonConvert.DeserializeObject<List<ProjectTemplate>>(value);
			}
		}
	}

	[Ignore]
	public List<ProjectTemplate> ProjectTemplates { get; set; }

	public DateTime UpdateTime { get; set; }
}
