using System.Collections.Generic;
using Newtonsoft.Json;

namespace ticktick_WPF.Models;

public class CourseArchivePostModel
{
	public List<CourseArchiveSyncModel> add { get; set; } = new List<CourseArchiveSyncModel>();

	public List<string> delete { get; set; } = new List<string>();

	[JsonIgnore]
	public bool Empty
	{
		get
		{
			if (add.Count == 0)
			{
				return delete.Count == 0;
			}
			return false;
		}
	}
}
