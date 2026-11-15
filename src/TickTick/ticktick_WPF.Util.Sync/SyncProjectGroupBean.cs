using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using ticktick_WPF.Models;

namespace ticktick_WPF.Util.Sync;

public class SyncProjectGroupBean
{
	private List<ProjectGroupModel> update = new List<ProjectGroupModel>();

	private List<ProjectGroupModel> add = new List<ProjectGroupModel>();

	private Collection<string> deleted = new Collection<string>();

	[JsonProperty(PropertyName = "update")]
	public List<ProjectGroupModel> Update
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
	public List<ProjectGroupModel> Add
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

	[JsonProperty(PropertyName = "deleted")]
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

	public bool Empty
	{
		get
		{
			if (Add.Count == 0 && Deleted.Count == 0)
			{
				return Update.Count == 0;
			}
			return false;
		}
	}
}
