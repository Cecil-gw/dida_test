using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncProjectBean
{
	private List<ProjectModel> update = new List<ProjectModel>();

	private List<ProjectModel> add = new List<ProjectModel>();

	private Collection<string> deleted = new Collection<string>();

	[JsonProperty(PropertyName = "update")]
	public List<ProjectModel> Update
	{
		get
		{
			return update;
		}
		set
		{
			update = value;
		}
	}

	[JsonProperty(PropertyName = "add")]
	public List<ProjectModel> Add
	{
		get
		{
			return add;
		}
		set
		{
			add = value;
		}
	}

	[JsonProperty(PropertyName = "delete")]
	public Collection<string> Deleted
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

	[JsonIgnore]
	public bool Empty
	{
		get
		{
			if (Add.Count == 0 && Update.Count == 0)
			{
				return Deleted.Count == 0;
			}
			return false;
		}
	}
}
