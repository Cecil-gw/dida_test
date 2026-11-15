using System.Collections.Generic;
using System.Linq;
using TickTickModels;
using ticktick_WPF.Cache;

namespace ticktick_WPF.Models;

public class ProjectUsersMode : BaseModel
{
	private string _projectId;

	public string projectId
	{
		get
		{
			return _projectId;
		}
		set
		{
			_projectId = value;
			SortOrder = CacheManager.GetProjects().FirstOrDefault((ProjectModel p) => p.ProjectId == value)?.sortOrder ?? 0;
		}
	}

	public List<ShareUserModel> shareUsers { get; set; }

	public long SortOrder { get; set; }
}
